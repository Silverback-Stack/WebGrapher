using Events.Core.Bus;
using Events.Core.Types;

namespace Crawler.Core
{
    public abstract class BaseCrawler : ICrawler
    {
        private readonly IEventBus _eventBus;

        internal const int DEFAULT_ABSOLUTE_EXPIRY_MINUTES = 60;
        internal const int DEFAULT_MAX_LINK_DEPTH = 5;

        protected BaseCrawler(IEventBus eventBus)
        {
            _eventBus = eventBus;
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

        private async Task HandleEvent(CrawlPageEvent evt)
        {
            if (CrawlPage(evt))
            {
                await _eventBus.PublishAsync(new ScrapePageEvent
                {
                    CrawlPageEvent = evt,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }
        private async Task HandleEvent(ScrapePageResultEvent evt)
        {
            //take the result information and store it to the cache
        }

        public bool CrawlPage(CrawlPageEvent evt)
        {

            //here add robots logic

            if (ReachedMaxDepth(evt.Depth, evt.MaxDepth))
                return false;

            if (GetUrl(evt.Url.AbsoluteUri) != null) 
                return false;

            SetUrl(evt.Url.AbsoluteUri, DateTimeOffset.UtcNow);

            return true;
        }

        internal abstract DateTimeOffset? GetUrl(string key);
        internal abstract void SetUrl(string key, DateTimeOffset value);

        public void Dispose()
        {
            _eventBus?.Dispose();
        }

        private bool ReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_LINK_DEPTH);
    }
}
