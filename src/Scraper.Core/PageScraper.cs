using System;
using System.Net;
using Events.Core.Bus;
using Events.Core.Dtos;
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
            var request = evt.CrawlPageRequest;

            var response = await FetchAsync(
                request.Url,
                request.UserAgent,
                request.UserAccepts);

            if (response is null)
            {
                _logger.LogDebug($"Scrape failed: No response for {request.Url} (Attempt {request.Attempt})");
                return;
            }

            if (response.Metadata.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogDebug($"Scrape failed: {request.Url} returned status {response.Metadata.StatusCode} Attempt {request.Attempt})");
                await PublishScrapePageFailedEvent(request, response);
                return;
            }

            if (response.IsFromCache)
            {
                _logger.LogDebug($"Skipped re-scrape for {request.Url} - found in cache.");
                return;
            }

            _logger.LogDebug($"Scraped page {request.Url} Attempt {request.Attempt} Status: {response.Metadata.StatusCode}");

            await PublishNormalisePageEvent(request, response);
        }

        private async Task PublishScrapePageFailedEvent(
            CrawlPageRequestDto request,
            HttpResponseEnvelope response)
        {
            await _eventBus.PublishAsync(new ScrapePageFailedEvent
            {
                CrawlPageRequest = request,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = response.Metadata.StatusCode,
                LastModified = response.Metadata.LastModified,
                RetryAfter = response.Metadata.RetryAfter
            }, priority: request.Depth);
        }

        private async Task PublishNormalisePageEvent(
            CrawlPageRequestDto request,
            HttpResponseEnvelope response)
        {
            
            var result = new ScrapePageResultDto
            {
                OriginalUrl = response.Metadata.OriginalUrl,
                Url = response.Metadata.Url,
                StatusCode = response.Metadata.StatusCode,
                IsRedirect = response.Metadata.IsRedirect,
                SourceLastModified = response.Metadata.LastModified,
                BlobId = response.Metadata.ResponseData?.BlobId,
                BlobContainer = response.Metadata.ResponseData?.BlobContainer,
                ContentType = response.Metadata.ResponseData?.ContentType,
                Encoding = response.Metadata.ResponseData?.Encoding,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _eventBus.PublishAsync(new NormalisePageEvent
            {
                CrawlPageRequest = request,
                ScrapePageResult = result,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: request.Depth);
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
