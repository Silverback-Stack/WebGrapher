using System;
using System.Net;
using Caching.Core;
using Crawler.Core.Policies;
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
        protected readonly IHistoryPolicy _historyPolicy;
        protected readonly IRateLimitPolicy _rateLimitPolicy;
        protected readonly IRobotsPolicy _robotsPolicy;

        protected const int DEFAULT_MAX_CRAWL_DEPTH = 5;

        public PageCrawler(
            ILogger logger, 
            IEventBus eventBus,
            ICache cache,
            IRequestSender requestSender,
            IHistoryPolicy historyPolicy,
            IRateLimitPolicy rateLimitPolicy,
            IRobotsPolicy robotsPolicy)
        {
            _eventBus = eventBus;
            _logger = logger;
            _cache = cache;
            _requestSender = requestSender;
            _historyPolicy = historyPolicy;
            _rateLimitPolicy = rateLimitPolicy;
            _robotsPolicy = robotsPolicy;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<CrawlPageEvent>(EventHandler);
            _eventBus.Subscribe<ScrapePageResultEvent>(EventHandler);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<CrawlPageEvent>(EventHandler);
            _eventBus.Unsubscribe<ScrapePageResultEvent>(EventHandler);
        }

        private async Task EventHandler(CrawlPageEvent evt)
        {
            await CrawlPage(evt);
        }
        private async Task EventHandler(ScrapePageResultEvent evt)
        {
            await _historyPolicy.SetResponseStatus(
                evt.CrawlPageEvent.Url,
                evt.StatusCode,
                evt.RetryAfter);

            //need a better check for retry policy here - TooManyRequests is not exclusive (check already written function)
            if (evt.StatusCode == HttpStatusCode.TooManyRequests)
                await PublishCrawlPageEvent(evt.CrawlPageEvent, evt.RetryAfter);
        }

        public async Task CrawlPage(CrawlPageEvent evt)
        {
            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
                return;

            var rateLimit = await _rateLimitPolicy.IsRateLimitedAsync(evt.Url);
            if (rateLimit.IsRateLimited)
            {
                await PublishCrawlPageEvent(evt, rateLimit.RetryAfter);
                return;
            }

            if (await _robotsPolicy.IsAllowedAsync(evt.Url, evt.UserAgent) == false)
                return;

            await PublishScrapePageEvent(evt);
        }

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);

        private async Task PublishCrawlPageEvent(CrawlPageEvent evt, DateTimeOffset? retryAfter)
        {
            var attempt = evt.Attempt + 1;
            await _eventBus.PublishAsync(new CrawlPageEvent(
                    evt,
                    evt.Url,
                    attempt,
                    evt.Depth), retryAfter);
        }

        private async Task PublishScrapePageEvent(CrawlPageEvent evt)
        {
            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageEvent = evt,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

    }
}
