using System;
using System.Net;
using System.Runtime.InteropServices;
using Caching.Core;
using Caching.Core.Helpers;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Logging.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Requests.Core;

namespace Crawler.Core
{
    public class PageCrawler : IPageCrawler, IEventBusLifecycle
    {
        protected readonly IEventBus _eventBus;
        protected readonly ILogger _logger;
        protected readonly ICache _cache;
        protected readonly IRequestSender _requestSender;
        protected readonly ISitePolicyResolver _sitePolicy;

        protected const int DEFAULT_MAX_CRAWL_ATTEMPTS = 3;
        protected const int DEFAULT_MAX_CRAWL_DEPTH = 5;
        protected const int SITE_POLICY_ABSOLUTE_EXPIRY_MINUTES = 20;

        public PageCrawler(
            ILogger logger,
            IEventBus eventBus,
            ICache cache,
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver)
        {
            _eventBus = eventBus;
            _logger = logger;
            _cache = cache;
            _requestSender = requestSender;
            _sitePolicy = sitePolicyResolver;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<CrawlPageEvent>(HandleCrawlPageEvent);
            _eventBus.Subscribe<ScrapePageFailedEvent>(HandleScrapePageFailedEvent);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<CrawlPageEvent>(HandleCrawlPageEvent);
            _eventBus.Unsubscribe<ScrapePageFailedEvent>(HandleScrapePageFailedEvent);
        }
        private async Task PublishScrapePageEvent(CrawlPageEvent evt)
        {
            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageEvent = evt,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private async Task PublishScheduledCrawlPageEvent(CrawlPageEvent evt, DateTimeOffset? retryAfter)
        {
            var attempt = evt.Attempt + 1;
            var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(retryAfter);

            await _eventBus.PublishAsync(new CrawlPageEvent(
                    evt,
                    evt.Url,
                    attempt,
                    evt.Depth), scheduledOffset);
        }

        private async Task HandleCrawlPageEvent(CrawlPageEvent evt)
        {
            await EvaluatePageForCrawling(evt);
        }
        private async Task HandleScrapePageFailedEvent(ScrapePageFailedEvent evt)
        {
            await RetryPageCrawl(evt);
        }

        /// <summary>
        /// Evaluates whether the page can be crawled based on retry limits, depth,
        /// rate limiting, and robots.txt permissions. If allowed, publishes a scrape event.
        /// </summary>
        public async Task EvaluatePageForCrawling(CrawlPageEvent evt)
        {
            if (HasExhaustedRetries(evt.Attempt))
            {
                _logger.LogInformation($"Abandoned: {evt.Url} reached the maximum number of retry attempts.");
                return;
            }

            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
            {
                _logger.LogInformation($"Stopped: {evt.Url} reached the maximum link depth to crawl and was stopped.");
                return;
            }

            var sitePolicy = await GetOrCreateSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts);

            //Is Site Rate Limited
            if (_sitePolicy.IsRateLimited(sitePolicy))
            {
                _logger.LogInformation($"Rate Limited: {evt.Url} will be crawled after {sitePolicy.RetryAfter?.ToString("HH:mm:ss")} with attempt {evt.Attempt}.");

                await PublishScheduledCrawlPageEvent(evt, sitePolicy.RetryAfter);
                await SetSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts, sitePolicy);
                return;
            }

            if (_sitePolicy.IsPermittedByRobotsTxt(evt.Url, evt.UserAgent, sitePolicy))
            {
                await PublishScrapePageEvent(evt);
            } 
            else
            {
                _logger.LogInformation($"Robots Denied: {evt.Url} could not be accessed due to RobotsTxt policy for user agent {evt.UserAgent}");
            }
                        
            await SetSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts, sitePolicy);
        }

        private async Task RetryPageCrawl(ScrapePageFailedEvent evt)
        {
            if (evt.RetryAfter is null) return;

            var sitePolicy = await GetOrCreateSitePolicyAsync(
                    evt.CrawlPageEvent.Url, evt.CrawlPageEvent.UserAgent, evt.CrawlPageEvent.UserAccepts);

            var newPolicy = sitePolicy with
            {
                RetryAfter = evt.RetryAfter
            };

            await SetSitePolicyAsync(
                evt.CrawlPageEvent.Url, 
                evt.CrawlPageEvent.UserAgent,
                evt.CrawlPageEvent.UserAccepts,
                newPolicy);

            _logger.LogInformation($"Retrying: {evt.CrawlPageEvent.Url} scheduled for retry after {newPolicy.RetryAfter?.ToString("HH:mm:ss")}");

            await PublishScheduledCrawlPageEvent(evt.CrawlPageEvent, newPolicy.RetryAfter);
        }


        private static bool HasExhaustedRetries(int currentAttempt) =>
            currentAttempt >= DEFAULT_MAX_CRAWL_ATTEMPTS;

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);


        private async Task<SitePolicyItem> GetOrCreateSitePolicyAsync(Uri url, string? userAgent, string? userAccepts)
        {
            var cacheKey = CacheKeyHelper.Generate(url.Authority, userAgent, userAccepts);

            var sitePolicy = await _cache.GetAsync<SitePolicyItem>(cacheKey);

            if (sitePolicy == null)
            {
                var robotsTxtContent = await _sitePolicy.GetRobotsTxtContentAsync(url, userAgent, userAccepts);

                sitePolicy = new SitePolicyItem
                {
                    UrlAuthority = url.Authority,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(SITE_POLICY_ABSOLUTE_EXPIRY_MINUTES),
                    RetryAfter = null,
                    RobotsTxtContent = robotsTxtContent
                };
            }

            await SetSitePolicyAsync(url, userAgent, userAccepts, sitePolicy);

            return sitePolicy;
        }

        /// <summary>
        /// PATTERN: Optermistic Concurrency with merge-on-write
        /// Rather than locking data as with pessimistic concurrency..
        /// Each service reads the latest version of the data, applies its changes and merges it with the latest version in the cache at write time
        /// </summary>
        private async Task SetSitePolicyAsync(Uri url, string? userAgent, string? userAccepts, SitePolicyItem? sitePolicy)
        {
            if (sitePolicy == null) return;

            var expiryDuration = TimeSpan.FromMinutes(SITE_POLICY_ABSOLUTE_EXPIRY_MINUTES);
            var cacheKey = CacheKeyHelper.Generate(url.Authority, userAgent, userAccepts);

            var existingSitePolicy = await _cache.GetAsync<SitePolicyItem>(cacheKey);

            if (existingSitePolicy != null)
                sitePolicy = existingSitePolicy.MergePolicy(sitePolicy);

            await _cache.SetAsync<SitePolicyItem>(cacheKey, sitePolicy, expiryDuration);
        }

    }
}
