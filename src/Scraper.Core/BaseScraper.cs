using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Logging.Core;
using Requests.Core;

namespace ScraperService
{

    public abstract class BaseScraper : IScraper, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;
        protected readonly IRequestSender _requestSender;

        public BaseScraper(ILogger logger, IEventBus eventBus, IRequestSender requestSender)
        {
            _logger = logger;
            _eventBus = eventBus;
            _requestSender = requestSender;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<ScrapePageEvent>(EventHandler);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<ScrapePageEvent>(EventHandler);
        }

        private async Task EventHandler(ScrapePageEvent evt)
        {
            var response = await GetAsync(
                evt.CrawlPageEvent.Url,
                evt.CrawlPageEvent.UserAgent,
                evt.CrawlPageEvent.UserAccepts);

            if (response != null)
            {
                await PublishScrapePageResultEvent(evt, response);

                if (response.StatusCode == HttpStatusCode.OK)
                    await PublishParsePageEvent(evt, response);
            }

            await Task.CompletedTask;
        }

        private async Task PublishScrapePageResultEvent(
            ScrapePageEvent evt, 
            ScrapeResponseItem response)
        {
            await _eventBus.PublishAsync(new ScrapePageFailedEvent()
            {
                CrawlPageEvent = evt.CrawlPageEvent,
                StatusCode = response.StatusCode,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModified = response.LastModified,
                RetryAfter = response.RetryAfter
            });
        }

        private async Task PublishParsePageEvent(
            ScrapePageEvent evt,
            ScrapeResponseItem response)
        {
            await _eventBus.PublishAsync(new ParsePageEvent
            {
                CrawlPageEvent = evt.CrawlPageEvent,
                CreatedAt = DateTimeOffset.UtcNow,
                HtmlContent = response.Content,
                LastModified = response.LastModified,
                StatusCode = response.StatusCode
            });
        }

        public abstract Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? userAccepts);

    }

}
