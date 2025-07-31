using System;
using System.Net;
using System.Text;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Scraper.Core
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
            _eventBus.Subscribe<ScrapePageEvent>(HandleScrapePageEvent);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<ScrapePageEvent>(HandleScrapePageEvent);
        }

        private async Task HandleScrapePageEvent(ScrapePageEvent evt)
        {
            await ScrapePage(evt);
        }

        private async Task ScrapePage(ScrapePageEvent evt)
        {
            var scrapeResponseItem = await GetAsync(
                evt.CrawlPageEvent.Url,
                evt.CrawlPageEvent.UserAgent,
                evt.CrawlPageEvent.UserAccepts);

            if (scrapeResponseItem is null)
                return;

            _logger.LogInformation($"Fetched: {evt.CrawlPageEvent.Url} Attempt {evt.CrawlPageEvent.Attempt} Status: {scrapeResponseItem.StatusCode} Cached: {scrapeResponseItem.IsFromCache}");

            if (scrapeResponseItem.StatusCode != HttpStatusCode.OK)
            {
                await PublishScrapePageFailedEvent(evt, scrapeResponseItem);
                return;
            }

            await PublishParsePageEvent(evt, scrapeResponseItem);
        }

        private async Task PublishScrapePageFailedEvent(
            ScrapePageEvent evt, 
            ScrapeResponseItem response)
        {
            await _eventBus.PublishAsync(new ScrapePageFailedEvent
            {
                CrawlPageEvent = evt.CrawlPageEvent,
                StatusCode = response.StatusCode,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModified = response.LastModified,
                RetryAfter = response.RetryAfter
            });
        }
        private async Task PublishParsePageEvent(
            ScrapePageEvent scrapeEvent,
            ScrapeResponseItem scrapeResponse)
        {
            await _eventBus.PublishAsync(new ParsePageEvent
            {
                CrawlPageEvent = scrapeEvent.CrawlPageEvent,
                Url = scrapeResponse.Url,
                Content = scrapeResponse.Content,
                LastModified = scrapeResponse.LastModified,
                StatusCode = scrapeResponse.StatusCode,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        public abstract Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? userAccepts);

    }

}
