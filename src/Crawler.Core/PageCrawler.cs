using System;
using Caching.Core;
using Caching.Core.Helpers;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Microsoft.Extensions.Logging;
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
        protected const int DEFAULT_MAX_CRAWL_DEPTH = 3;
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
            _eventBus.Subscribe<CrawlPageEvent>(EvaluatePageForCrawling);
            _eventBus.Subscribe<ScrapePageFailedEvent>(RetryPageCrawl);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<CrawlPageEvent>(EvaluatePageForCrawling);
            _eventBus.Unsubscribe<ScrapePageFailedEvent>(RetryPageCrawl);
        }

        /// <summary>
        /// Evaluates whether the page can be crawled based on retry limits, depth,
        /// rate limiting, and robots.txt permissions. If allowed, publishes a scrape event.
        /// </summary>
        public async Task EvaluatePageForCrawling(CrawlPageEvent evt)
        {
            _logger.LogInformation($"Crawl requested: {evt.Url} Depth: {evt.Depth} Attempt: {evt.Attempt}");

            if (HasExhaustedRetries(evt.Attempt))
            {
                _logger.LogDebug($"Abandoned crawl: {evt.Url} max retries reached: {evt.Attempt})");
                return;
            }

            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
            {
                _logger.LogDebug($"Stopped crawl: {evt.Url} max depth reached: {evt.Depth})");
                return;
            }

            var sitePolicy = await GetOrCreateSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts);

            if (_sitePolicy.IsRateLimited(sitePolicy))
            {
                _logger.LogDebug($"Rate limited: {evt.Url} retry scheduled after: {sitePolicy.RetryAfter?.ToString("o")} Attempt: {evt.Attempt}");

                await PublishScheduledCrawlPageEvent(evt, sitePolicy.RetryAfter);
                await SetSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts, sitePolicy);
                return;
            }

            if (_sitePolicy.IsPermittedByRobotsTxt(evt.Url, evt.UserAgent, sitePolicy))
            {
                _logger.LogDebug($"Permitted by robots.txt: {evt.Url}, publishing scrape event. Depth: {evt.Depth}");

                await PublishScrapePageEvent(evt);
            } 
            else
            {
                _logger.LogDebug($"Robots.txt denied: {evt.Url} for user agent '{evt.UserAgent}'");
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

            _logger.LogDebug($"Scheduling retry: {evt.CrawlPageEvent.Url} after {newPolicy.RetryAfter?.ToString("o")}. Next attempt: {evt.CrawlPageEvent.Attempt + 1}");

            await PublishScheduledCrawlPageEvent(evt.CrawlPageEvent, newPolicy.RetryAfter);
        }

        private static bool HasExhaustedRetries(int currentAttempt) =>
            currentAttempt >= DEFAULT_MAX_CRAWL_ATTEMPTS;

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);

        private async Task PublishScrapePageEvent(CrawlPageEvent evt)
        {
            _logger.LogDebug($"Publishing ScrapePageEvent: {evt.Url} Depth: {evt.Depth} Attempt: {evt.Attempt}");

            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageEvent = evt,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: evt.Depth);
        }

        private async Task PublishScheduledCrawlPageEvent(CrawlPageEvent evt, DateTimeOffset? retryAfter)
        {
            var attempt = evt.Attempt + 1;
            var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(retryAfter);

            _logger.LogInformation($"Scheduled CrawlPageEvent: {evt.Url} at depth {evt.Depth}, attempt {attempt}, scheduled for {scheduledOffset?.ToString("o")}");

            await _eventBus.PublishAsync(new CrawlPageEvent(
                evt, evt.Url, attempt, evt.Depth), 
                    priority: evt.Depth, scheduledOffset);
        }

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

                _logger.LogDebug($"New site policy created for: {url.Authority}");
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

            _logger.LogDebug($"Saving policy for: {url.Authority}, expires at: {sitePolicy?.ExpiresAt.ToString("o")}");

            await _cache.SetAsync<SitePolicyItem>(cacheKey, sitePolicy, expiryDuration);
        }

    }
}
