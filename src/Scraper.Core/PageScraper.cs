using System;
using System.Net;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Scraper.Core
{
    public class PageScraper : IPageScraper, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;
        protected readonly IRequestSender _requestSender;
        protected readonly ScraperSettings _scraperSettings;

        public PageScraper(ILogger logger, IEventBus eventBus, IRequestSender requestSender, ScraperSettings scraperSettings)
        {
            _logger = logger;
            _eventBus = eventBus;
            _requestSender = requestSender;
            _scraperSettings = scraperSettings;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<ScrapePageEvent>(ScrapeContentAsync);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<ScrapePageEvent>(ScrapeContentAsync);
        }

        public async Task PublishClientLogEventAsync(
            Guid graphId,
            Guid? correlationId,
            LogType type,
            string message,
            string? code = null,
            Object? context = null)
        {
            var clientLogEvent = new ClientLogEvent
            {
                GraphId = graphId,
                CorrelationId = correlationId,
                Type = type,
                Message = message,
                Code = code,
                Service = _scraperSettings.ServiceName,
                Context = context
            };

            await _eventBus.PublishAsync(clientLogEvent);
        }

        private async Task ScrapeContentAsync(ScrapePageEvent evt)
        {
            string logMessage;
            var request = evt.CrawlPageRequest;

            var response = await FetchAsync(
                request.Url,
                request.Options.UserAgent,
                request.Options.UserAccepts,
                request.RequestCompositeKey);

            if (response is null)
            {
                logMessage = $"Scrape Failed: {request.Url} yielded no response. Attempt: {request.Attempt}";
                _logger.LogError(logMessage);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    logMessage,
                    "ScrapeFailed",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Attempt = request.Attempt
                    });
                return;
            }

            if (response.Metadata.StatusCode != HttpStatusCode.OK)
            {
                await PublishScrapePageFailedEventAsync(request, response);

                logMessage = $"Scrape Failed: {request.Url} Status: {response.Metadata.StatusCode}. Attempt: {request.Attempt}";
                _logger.LogError(logMessage);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    logMessage,
                    "ScrapeFailed",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Attempt = request.Attempt,
                        StatusCode = response.Metadata.StatusCode.ToString()
                    });
                return;
            }

            await PublishNormalisePageEventAsync(request, response);

            var source = response.IsFromCache ? "Cache" : "Live";
            logMessage = $"Scrape Completed ({source}): {request.Url} Status: {response.Metadata.StatusCode}. Attempt {request.Attempt}";
            _logger.LogInformation(logMessage);

            await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Information,
                    logMessage,
                    "ScrapeCompleted",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Attempt = request.Attempt,
                        StatusCode = response.Metadata.StatusCode.ToString()
                    });
        }

        private async Task PublishScrapePageFailedEventAsync(
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

        private async Task PublishNormalisePageEventAsync(
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

        public async Task<HttpResponseEnvelope?> FetchAsync(
            Uri url,
            string userAgent,
            string clientAccept,
            string compositeKey = "",
            CancellationToken cancellationToken = default)
        {
            return await _requestSender.FetchAsync(
                url,
                userAgent,
                clientAccept,
                _scraperSettings.ContentMaxBytes,
                compositeKey,
                cancellationToken);
        }
    }

}
