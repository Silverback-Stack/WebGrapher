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
            //when a scraped page returns response other than OK
            //publish ScrapePageFailedEvent <- notice the rename to failed!

            //when a page fails for RateLimiting:
            //get policy from cache
            //update retry after <- only update if it is later than value currently set!!
            //update modified date
            //set policy to cache (only if data changed)

            //if policy_isRateLimited:
            //schedule page crawl event in the future!!
            await PublishCrawlPageEvent(evt.CrawlPageEvent, evt.RetryAfter);
                
        }

        private async Task PublishScrapePageEvent(CrawlPageEvent evt)
        {
            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageEvent = evt,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private async Task PublishCrawlPageEvent(CrawlPageEvent evt, DateTimeOffset? retryAfter)
        {
            var attempt = evt.Attempt + 1;
            await _eventBus.PublishAsync(new CrawlPageEvent(
                    evt,
                    evt.Url,
                    attempt,
                    evt.Depth), retryAfter);
        }

        public async Task EvaluatePageForCrawling(CrawlPageEvent evt)
        {
            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
                return;

            var sitePolicy = await GetSitePolicyAsync(evt.Url, evt.UserAgent, evt.UserAccepts);

            if (_sitePolicy.IsRateLimited(sitePolicy))
                await PublishCrawlPageEvent(evt, sitePolicy.RetryAfter);

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
                    FetchedAt = DateTimeOffset.UtcNow,
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
