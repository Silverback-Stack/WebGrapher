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
        protected readonly IRateLimitPolicy _rateLimitPolicy;
        protected readonly IRobotsPolicy _robotsPolicy;

        protected const int DEFAULT_ABSOLUTE_EXPIRY_MINUTES = 60;
        protected const int DEFAULT_MAX_CRAWL_DEPTH = 5;

        public PageCrawler(
            ILogger logger, 
            IEventBus eventBus,
            ICache cache,
            IRequestSender requestSender)
        {
            _eventBus = eventBus;
            _logger = logger;
            _cache = cache;
            _requestSender = requestSender;

            _rateLimitPolicy = new RateLimitPolicy(logger, cache, requestSender);
            _robotsPolicy = new RobotsPolicy(logger, cache, requestSender);
        }

        public void Start()
        {
            Subscribe();
        }

        public void Subscribe()
        {
            _eventBus.Subscribe<CrawlPageEvent>(EventHandler);
            _eventBus.Subscribe<ScrapePageResultEvent>(EventHandler);
        }

        public void Unsubscribe()
        {
            _eventBus.Unsubscribe<CrawlPageEvent>(EventHandler);
            _eventBus.Unsubscribe<ScrapePageResultEvent>(EventHandler);
        }

        private async Task EventHandler(CrawlPageEvent evt)
        {
            await CrawlPage(evt);
            await Task.CompletedTask;
        }
        private async Task EventHandler(ScrapePageResultEvent evt)
        {
            //if 429 - too many requests
            //publish CrawlPageEvent with delay

            //SetHistory(url); //store the result of request with expiry date or default expiry if shorter
            await Task.CompletedTask;
        }

        public async Task CrawlPage(CrawlPageEvent evt)
        {
            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
                return;

            if (_rateLimitPolicy.IsRateLimited(evt.Url))
                return;

            if (await _robotsPolicy.IsAllowed(evt.Url, evt.UserAgent) == false)
                return;

            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageEvent = evt,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);

    }
}
