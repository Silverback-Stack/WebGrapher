using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Logging.Core;

namespace ScraperService
{
    public abstract class BaseScraper : IScraper, IEventBusLifecycle
    {
        internal readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        public BaseScraper(ILogger logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public void Start()
        {
            Subscribe();
        }

        public void Subscribe()
        {
            _eventBus.Subscribe<ScrapePageEvent>(EventHandler);
        }

        public void Unsubscribe()
        {
            _eventBus.Unsubscribe<ScrapePageEvent>(EventHandler);
        }

        private async Task EventHandler(ScrapePageEvent evt)
        {
            var responseDto = await GetAsync(
                evt.CrawlPageEvent.Url, 
                evt.CrawlPageEvent.UserAgent, 
                evt.CrawlPageEvent.ClientAccepts);

            if (responseDto != null && responseDto.StatusCode == HttpStatusCode.OK)
            {
                await _eventBus.PublishAsync(new ParsePageEvent
                {
                    CrawlPageEvent = evt.CrawlPageEvent,
                    CreatedAt = DateTimeOffset.UtcNow,
                    HtmlContent = responseDto.HtmlContent,
                    LastModified = responseDto.LastModified,
                    StatusCode = responseDto.StatusCode
                });
            }
            await Task.CompletedTask;
        }

        public abstract Task<ScrapeResponse> GetAsync(Uri url, string userAgent, string clientAccept);

    }

}
