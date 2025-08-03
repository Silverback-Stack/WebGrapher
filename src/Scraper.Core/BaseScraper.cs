using System;
using System.Net;
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
            _eventBus.Subscribe<ScrapePageEvent>(ScrapePage);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<ScrapePageEvent>(ScrapePage);
        }

        private async Task ScrapePage(ScrapePageEvent evt)
        {
            var scrapeResponseItem = await GetAsync(
                evt.CrawlPageEvent.Url,
                evt.CrawlPageEvent.UserAgent,
                evt.CrawlPageEvent.UserAccepts);

            if (scrapeResponseItem is null)
            {
                _logger.LogDebug($"Scrape failed: No response for {evt.CrawlPageEvent.Url} (Attempt {evt.CrawlPageEvent.Attempt})");
                return;
            }

            if (scrapeResponseItem.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogDebug($"Scrape failed: {evt.CrawlPageEvent.Url} returned status {scrapeResponseItem.StatusCode} Attempt {evt.CrawlPageEvent.Attempt})");
                await PublishScrapePageFailedEvent(evt, scrapeResponseItem);
                return;
            }

            if (!scrapeResponseItem.IsFromCache)
            {
                await PublishParsePageEvent(evt, scrapeResponseItem);
            }

            _logger.LogDebug($"Scraped page {evt.CrawlPageEvent.Url} Attempt {evt.CrawlPageEvent.Attempt} Status: {scrapeResponseItem.StatusCode} Cached: {scrapeResponseItem.IsFromCache}");
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
            }, priority: evt.CrawlPageEvent.Depth);
        }
        private async Task PublishParsePageEvent(
            ScrapePageEvent scrapeEvent,
            ScrapeResponseItem scrapeResponse)
        {
            await _eventBus.PublishAsync(new ParsePageEvent
            {
                CrawlPageEvent = scrapeEvent.CrawlPageEvent,
                Url = scrapeResponse.ResolvedUrl,
                Content = scrapeResponse.Content,
                LastModified = scrapeResponse.LastModified,
                StatusCode = scrapeResponse.StatusCode,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: scrapeEvent.CrawlPageEvent.Depth);
        }

        public abstract Task<ScrapeResponseItem?> GetAsync(Uri url, string? userAgent, string? userAccepts);

    }

}
