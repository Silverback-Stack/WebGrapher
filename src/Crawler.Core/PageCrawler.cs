using System;
using System.Net;
using System.Runtime.InteropServices;
using Caching.Core;
using Caching.Core.Helpers;
using Events.Core.Bus;
using Events.Core.EventTypes;
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
            await _eventBus.PublishAsync(new CrawlPageEvent(
                    evt,
                    evt.Url,
                    attempt,
                    evt.Depth), retryAfter);
            _logger.LogInformation($"Rate Limited: {evt.Url} will be crawled after {retryAfter?.ToString("HH:mm:ss")} with attempt {attempt}.");
        }

        private async Task HandleCrawlPageEvent(CrawlPageEvent evt)
        {
            await EvaluatePageForCrawling(evt);
        }
        private async Task HandleScrapePageFailedEvent(ScrapePageFailedEvent evt)
        {
            await RetryPageCrawl(evt);
        }


        public async Task EvaluatePageForCrawling(CrawlPageEvent evt)
        {
            if (HasReachedMaxAttempt(evt.Attempt) || 
                HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
                return; //TODO: Log inforamtiont hat request was abandoned due to either attempts or link depth

            var sitePolicy = await GetOrCreateSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts);

            if (_sitePolicy.IsRateLimited(sitePolicy))
            {
                await PublishScheduledCrawlPageEvent(evt, sitePolicy.RetryAfter);
            }
            else
            {
                var robotsTxt = sitePolicy.RobotsTxtContent;
                if (robotsTxt is null)
                {
                    robotsTxt = await _sitePolicy.GetRobotsTxtContentAsync(evt.Url, evt.UserAgent, evt.UserAccepts);
                    sitePolicy = sitePolicy with
                    {
                        RobotsTxtContent = robotsTxt
                    };
                }
                if (_sitePolicy.IsPermittedByRobotsTxt(
                    evt.Url, evt.UserAgent, sitePolicy))
                {
                    await PublishScrapePageEvent(evt);
                }
            }

            await SetSitePolicyAsync(
                evt.Url, evt.UserAgent, evt.UserAccepts, sitePolicy);
        }

        private async Task RetryPageCrawl(ScrapePageFailedEvent evt)
        {
            if (evt.RetryAfter is null) return;

            var sitePolicy = await GetOrCreateSitePolicyAsync(
                    evt.CrawlPageEvent.Url, evt.CrawlPageEvent.UserAgent, evt.CrawlPageEvent.UserAccepts);

            if (sitePolicy.RetryAfter is null ||
                sitePolicy.RetryAfter < evt.RetryAfter)
            {
                sitePolicy = sitePolicy with
                {
                    RetryAfter = evt.RetryAfter,
                    ModifiedAt = DateTimeOffset.UtcNow
                };
            };

            await SetSitePolicyAsync(
                evt.CrawlPageEvent.Url, 
                evt.CrawlPageEvent.UserAgent,
                evt.CrawlPageEvent.UserAccepts, 
                sitePolicy);

            await PublishScheduledCrawlPageEvent(evt.CrawlPageEvent, evt.RetryAfter);
        }


        private static bool HasReachedMaxAttempt(int currentAttempt) =>
            currentAttempt >= DEFAULT_MAX_CRAWL_ATTEMPTS;

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);


        private async Task<SitePolicyItem> GetOrCreateSitePolicyAsync(Uri url, string? userAgent, string? userAccepts)
        {
            var cacheKey = CacheKeyHelper.Generate(url.Authority, userAgent, userAccepts);

            var sitePolicy = await _cache.GetAsync<SitePolicyItem>(cacheKey);

            if (sitePolicy == null)
            {
                sitePolicy = new SitePolicyItem
                {
                    UrlAuthority = url.Authority,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(SITE_POLICY_ABSOLUTE_EXPIRY_MINUTES),
                    RetryAfter = null,
                    RobotsTxtContent = null
                };
            }

            await SetSitePolicyAsync(url, userAgent, userAccepts, sitePolicy);

            return sitePolicy;
        }



        //TODO: refacter this method - see Copilot chat
        private async Task SetSitePolicyAsync(Uri url, string? userAgent, string? userAccepts, SitePolicyItem? sitePolicy)
        {
            if (sitePolicy == null) return;

            var expiryDuration = TimeSpan.FromMinutes(SITE_POLICY_ABSOLUTE_EXPIRY_MINUTES);
            var cacheKey = CacheKeyHelper.Generate(url.Authority, userAgent, userAccepts);

            var existingSitePolicy = await _cache.GetAsync<SitePolicyItem>(cacheKey);
            if (existingSitePolicy == null)
            {
                await _cache.SetAsync<SitePolicyItem>(cacheKey, sitePolicy, expiryDuration);
                return;
            }

            //Merge RetryAfter to resolve clash:
            DateTimeOffset? retryAfter = null;
            if (existingSitePolicy.RetryAfter is null)
            {
                retryAfter = sitePolicy.RetryAfter;
            }
            else if (sitePolicy.RetryAfter is null)
            {
                retryAfter = existingSitePolicy.RetryAfter;
            }
            else
            {
                retryAfter = existingSitePolicy.RetryAfter > sitePolicy.RetryAfter
                    ? existingSitePolicy.RetryAfter
                    : sitePolicy.RetryAfter;
            }


            var robotsTxtContent = existingSitePolicy.RobotsTxtContent is null &&
                sitePolicy.RobotsTxtContent is not null
                ? sitePolicy.RobotsTxtContent
                : existingSitePolicy.RobotsTxtContent;


            var mergedPolicy = existingSitePolicy with
            {
                RetryAfter = retryAfter,
                RobotsTxtContent = robotsTxtContent
            };

            await _cache.SetAsync<SitePolicyItem>(cacheKey, mergedPolicy, expiryDuration);
        }

    }
}
