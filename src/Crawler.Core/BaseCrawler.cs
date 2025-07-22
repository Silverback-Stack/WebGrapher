using Caching.Core;
using Crawler.Core.RobotsEvaluator;
using Events.Core.Bus;
using Events.Core.Types;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core
{
    public abstract class BaseCrawler : ICrawler
    {
        protected readonly IEventBus _eventBus;
        protected readonly IAppLogger _logger;
        protected readonly ICache _cache;
        protected readonly IRequestSender _requestSender;
        protected readonly IRobotsEvaluator _robotsService;

        protected const int DEFAULT_ABSOLUTE_EXPIRY_MINUTES = 60;
        protected const int DEFAULT_MAX_LINK_DEPTH = 5;

        protected BaseCrawler(
            IAppLogger logger, 
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

        async Task IEventBusLifecycle.StartAsync()
        {
            await _eventBus.StartAsync();

            _eventBus.Subscribe<CrawlPageEvent>(async evt =>
            {
                await HandleEvent(evt);
                await Task.CompletedTask;
            });

            _eventBus.Subscribe<ScrapePageResultEvent>(async evt =>
            {
                await HandleEvent(evt);
                await Task.CompletedTask;
            });
        }

        async Task IEventBusLifecycle.StopAsync()
        {
            await _eventBus.StopAsync();
        }

        public void Dispose()
        {
            _eventBus?.Dispose();
            _logger?.Dispose();
        }

        private async Task HandleEvent(CrawlPageEvent evt)
        {
            await CrawlPage(evt);
        }
        private async Task HandleEvent(ScrapePageResultEvent evt)
        {
            //if 429 - too many requests
            //publish CrawlPageEvent with delay

            //SetHistory(url); //store the result of request with expiry date or default expiry if shorter
        }

        public async Task CrawlPage(CrawlPageEvent evt)
        {
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
