using Caching.Core;
using Crawler.Core.RobotsEvaluator;
using Events.Core.Bus;
using Events.Core.Types;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core
{
    public abstract class BaseCrawler : ICrawler, IEventBusLifecycle
    {
        protected readonly IEventBus _eventBus;
        protected readonly ILogger _logger;
        protected readonly ICache _cache;
        protected readonly IRequestSender _requestSender;
        protected readonly IRobotsEvaluator _robotsService;

        protected const int DEFAULT_ABSOLUTE_EXPIRY_MINUTES = 60;
        protected const int DEFAULT_MAX_LINK_DEPTH = 5;

        protected BaseCrawler(
            ILogger logger, 
            IEventBus eventBus,
            ICache cache,
            IRequestSender requestSender,
            IRobotsEvaluator robotsEvaluator)
        {
            _eventBus = eventBus;
            _logger = logger;
            _cache = cache;
            _requestSender = requestSender;
            _robotsService = robotsEvaluator;
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
            _logger.LogWarning($"Crawling page {evt.Url}");

            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
                return;

            if (!await _robotsService.IsUrlPermittedAsync(evt.Url, evt.UserAgent))
                return;

            var history = await GetHistory(evt.Url);
            if (history)
            {
                //decide if we can crawl
                //ELSE
                return;
            }

            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageEvent = evt,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        protected abstract Task<bool> GetHistory(Uri url);

        protected abstract Task SetHistory(Uri url);

        //internal abstract DateTimeOffset? GetUrl(string key);
        //internal abstract void SetUrl(string key, DateTimeOffset value);

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_LINK_DEPTH);

    }
}
