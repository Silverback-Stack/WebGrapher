using System;
using System.Net;
using Caching.Core;
using Caching.Core.Helpers;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Logging.Core;
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
            _eventBus.Subscribe<CrawlPageEvent>(EventHandler);
            _eventBus.Subscribe<ScrapePageFailedEvent>(EventHandler);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<CrawlPageEvent>(EventHandler);
            _eventBus.Unsubscribe<ScrapePageFailedEvent>(EventHandler);
        }

        private async Task EventHandler(CrawlPageEvent evt)
        {
            await EvaluatePageForCrawling(evt);
        }
        private async Task EventHandler(ScrapePageFailedEvent evt)
        {
            await RetryPageCrawl(evt);
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
        }

        private async Task RetryPageCrawl(ScrapePageFailedEvent evt)
        {
            if (evt.RetryAfter is null) return;

            var sitePolicy = await GetSitePolicyAsync(
                    evt.CrawlPageEvent.Url, evt.CrawlPageEvent.UserAgent, evt.CrawlPageEvent.UserAccepts);

            if (sitePolicy.RetryAfter is not null &&
                sitePolicy.RetryAfter < evt.RetryAfter)
            {
                sitePolicy = sitePolicy with
                {
                    RetryAfter = evt.RetryAfter,
                    ModifiedAt = DateTimeOffset.UtcNow
                };
            };

            await SetSitePolicyAsync(evt.CrawlPageEvent.Url, evt.CrawlPageEvent.UserAgent, evt.CrawlPageEvent.UserAccepts, sitePolicy);

            //schedule future page crawl event honoring site RetryAfter requirement
            await PublishScheduledCrawlPageEvent(evt.CrawlPageEvent, evt.RetryAfter);

            _logger.LogInformation($"Crawl Scheduled: {evt.CrawlPageEvent.Url} has been scheduled to be crawled at {evt.RetryAfter.Value:HH:mm}.");
        }

        public async Task EvaluatePageForCrawling(CrawlPageEvent evt)
        {
            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
                return;

            var sitePolicy = await GetSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts);

            if (_sitePolicy.IsRateLimited(sitePolicy))
                await PublishScheduledCrawlPageEvent(evt, sitePolicy.RetryAfter);

            else if (await _sitePolicy.IsPermittedByRobotsTxt(
                evt.Url, evt.UserAgent, evt.UserAccepts, sitePolicy))
                await PublishScrapePageEvent(evt);

            await SetSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts, sitePolicy);
        }
        
        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);

        private async Task<SitePolicyItem> GetSitePolicyAsync(Uri url, string? userAgent, string? userAccepts)
        {
            var cacheKey = CacheKeyHelper.Generate(url, userAgent, userAccepts);
            var sitePolicy = await _cache.GetAsync<SitePolicyItem?>(cacheKey);
            if (sitePolicy == null)
            {
                sitePolicy = new SitePolicyItem()
                {
                    UrlAuthority = url.Authority,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(SITE_POLICY_ABSOLUTE_EXPIRY_MINUTES),
                    RetryAfter = null,
                    RobotsTxtContent = null
                };
            }
            return sitePolicy;
        }

        private async Task SetSitePolicyAsync(Uri url, string? userAgent, string? userAccepts, SitePolicyItem? sitePolicy)
        {
            if (sitePolicy != null) {
                var cacheKey = CacheKeyHelper.Generate(url, userAgent, userAccepts);
                var expiryDuration = TimeSpan.FromMinutes(SITE_POLICY_ABSOLUTE_EXPIRY_MINUTES);
                await _cache.SetAsync<SitePolicyItem>(cacheKey, sitePolicy, expiryDuration);
            }
        }

    }
}
