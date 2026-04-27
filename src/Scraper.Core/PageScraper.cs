using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Microsoft.Extensions.Logging;
using Requests.Core;
using SitePolicy.Core;
using System;
using System.Net;

namespace Scraper.Core
{
    public class PageScraper : IPageScraper, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;
        protected readonly IRequestSender _requestSender;
        protected readonly ISitePolicyResolver _sitePolicyResolver;
        protected readonly ScraperSettings _scraperSettings;

        public PageScraper(ILogger logger, 
            IEventBus eventBus, 
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver,
            ScraperSettings scraperSettings)
        {
            _logger = logger;
            _eventBus = eventBus;
            _requestSender = requestSender;
            _sitePolicyResolver = sitePolicyResolver;
            _scraperSettings = scraperSettings;
        }

        public async Task StartAsync()
        {
            await _eventBus.SubscribeAsync<ScrapePageEvent>(_scraperSettings.ServiceName, ScrapeContentAsync);
        }

        public async Task StopAsync()
        {
            await _eventBus.UnsubscribeAsync<ScrapePageEvent>(_scraperSettings.ServiceName, ScrapeContentAsync);
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
            var request = evt.CrawlPageRequest;
            string logMessage;


            // Check if the site is currently rate-limited for this request sender's partition.
            var limitedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                request.Url,
                request.Options.UserAgent,
                _requestSender.PartitionKey);

            if (limitedUntil is not null)
            {
                await HandlePageFailedFromRateLimitAsync(request, limitedUntil);
                return;
            }


            // Make request to fetch page
            var response = await FetchAsync(
                request.Url,
                request.Options.UserAgent,
                request.Options.UserAccepts,
                request.RequestCompositeKey);


            if (response is null)
            {
                await HandlePageFailedNoResponseAsync(request);
                return;
            }

            if (response.Metadata.StatusCode != HttpStatusCode.OK)
            {
                await HandlePageFailedFromResponseAsync(request, response);
                return;
            }


            await PublishNormalisePageEventAsync(request, response);

            var source = response.Cache?.IsFromCache == true ? "Cache" : "Live";
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


        private async Task HandlePageFailedNoResponseAsync(
            CrawlPageRequestDto request)
        {
            var failedEvent = new ScrapePageFailedEvent
            {
                CrawlPageRequest = request,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = HttpStatusCode.ServiceUnavailable,
                PartitionKey = _requestSender.PartitionKey
            };

            await PublishScrapePageFailedAsync(request, failedEvent);
        }


        private async Task HandlePageFailedFromRateLimitAsync(
            CrawlPageRequestDto request,
            DateTimeOffset? limitedUntil)
        {
            var failedEvent = new ScrapePageFailedEvent
            {
                CrawlPageRequest = request,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = HttpStatusCode.TooManyRequests,
                RetryAfter = limitedUntil,
                PartitionKey = _requestSender.PartitionKey
            };

            await PublishScrapePageFailedAsync(request, failedEvent);
        }


        private async Task HandlePageFailedFromResponseAsync(
            CrawlPageRequestDto request,
            HttpResponseEnvelope response)
        {
            var failedEvent = new ScrapePageFailedEvent
            {
                CrawlPageRequest = request,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = response.Metadata.StatusCode,
                LastModified = response.Metadata.LastModified,
                RetryAfter = response.Metadata.RetryAfter,
                PartitionKey = response.Cache?.PartitionKey ?? _requestSender.PartitionKey
            };

            await PublishScrapePageFailedAsync(request, failedEvent);
        }


        private async Task PublishScrapePageFailedAsync(
            CrawlPageRequestDto request,
            ScrapePageFailedEvent failedEvent)
        {
            if (IsRetryableFailure(failedEvent.StatusCode))
                await _eventBus.PublishAsync(failedEvent, priority: request.Depth);

            var logMessage = $"Scrape Failed: {request.Url} Status: {failedEvent.StatusCode}. Attempt: {request.Attempt}";
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
                    StatusCode = failedEvent.StatusCode.ToString()
                });
        }


        /// <summary>
        /// Determines whether a failure is transient and should be retried.
        /// </summary>
        private static bool IsRetryableFailure(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.TooManyRequests
                || statusCode == HttpStatusCode.InternalServerError
                || statusCode == HttpStatusCode.BadGateway
                || statusCode == HttpStatusCode.ServiceUnavailable
                || statusCode == HttpStatusCode.GatewayTimeout;
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
                BlobId = response.Cache?.Key,
                BlobContainer = response.Cache?.Container,
                ContentType = response.Metadata.ContentType,
                Encoding = response.Metadata.Encoding,
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
