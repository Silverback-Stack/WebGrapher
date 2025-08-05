using System;
using System.Net;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Scraper.Core
{
    public class PageScraper : IPageScraper, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;
        protected readonly IRequestSender _requestSender;

        private const int DEFAULT_CONTENT_MAX_BYTES = 4_194_304; //4 Mb

        public PageScraper(ILogger logger, IEventBus eventBus, IRequestSender requestSender)
        {
            _logger = logger;
            _eventBus = eventBus;
            _requestSender = requestSender;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<ScrapePageEvent>(ScrapeContent);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<ScrapePageEvent>(ScrapeContent);
        }

        private async Task ScrapeContent(ScrapePageEvent evt)
        {
            var response = await FetchAsync(
                evt.CrawlPageEvent.Url,
                evt.CrawlPageEvent.UserAgent,
                evt.CrawlPageEvent.UserAccepts);

            if (response is null)
            {
                _logger.LogDebug($"Scrape failed: No response for {evt.CrawlPageEvent.Url} (Attempt {evt.CrawlPageEvent.Attempt})");
                return;
            }

            if (response.Metadata.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogDebug($"Scrape failed: {evt.CrawlPageEvent.Url} returned status {response.Metadata.StatusCode} Attempt {evt.CrawlPageEvent.Attempt})");
                await PublishScrapePageFailedEvent(evt, response);
                return;
            }

            if (response.IsFromCache)
            {
                _logger.LogDebug($"Skipped re-scrape for {evt.CrawlPageEvent.Url} - found in cache.");
                return;
            }

            _logger.LogDebug($"Scraped page {evt.CrawlPageEvent.Url} Attempt {evt.CrawlPageEvent.Attempt} Status: {response.Metadata.StatusCode}");

            await PublishNormalisePageEvent(evt, response);
        }

        private async Task PublishScrapePageFailedEvent(
            ScrapePageEvent evt,
            HttpResponseEnvelope response)
        {
            await _eventBus.PublishAsync(new ScrapePageFailedEvent
            {
                CrawlPageEvent = evt.CrawlPageEvent,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = response.Metadata.StatusCode,
                LastModified = response.Metadata.LastModified,
                RetryAfter = response.Metadata.RetryAfter
            }, priority: evt.CrawlPageEvent.Depth);
        }

        private async Task PublishNormalisePageEvent(
            ScrapePageEvent scrapeEvent,
            HttpResponseEnvelope response)
        {
            await _eventBus.PublishAsync(new NormalisePageEvent
            {
                CrawlPageEvent = scrapeEvent.CrawlPageEvent,
                OriginalUrl = response.Metadata.OriginalUrl,
                Url = response.Metadata.Url,
                IsRedirect = response.Metadata.IsRedirect,
                BlobId = response.Metadata.ResponseData?.BlobId,
                BlobContainer = response.Metadata.ResponseData?.BlobContainer,
                ContentType = response.Metadata.ResponseData?.ContentType,
                Encoding = response.Metadata.ResponseData?.Encoding,
                LastModified = response.Metadata.LastModified,
                StatusCode = response.Metadata.StatusCode,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: scrapeEvent.CrawlPageEvent.Depth);
        }

        public async Task<HttpResponseEnvelope?> FetchAsync(Uri url, string? userAgent, string? clientAccept)
        {
            return await _requestSender.FetchAsync(
                url,
                userAgent,
                clientAccept,
                DEFAULT_CONTENT_MAX_BYTES);
        }

    }

}
