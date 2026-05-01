using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Events.Core.Helpers;
using Microsoft.Extensions.Logging;
using Requests.Core;
using SitePolicy.Core;
using System;

namespace Crawler.Core
{
    public class PageCrawler : IPageCrawler, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;
        protected readonly IRequestSender _requestSender;
        protected readonly ISitePolicyResolver _sitePolicyResolver;
        protected readonly CrawlerSettings _crawlerSettings;

        public PageCrawler(
            ILogger logger,
            IEventBus eventBus,
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver,
            CrawlerSettings crawlerSettings)
        {
            _eventBus = eventBus;
            _logger = logger;
            _requestSender = requestSender;
            _sitePolicyResolver = sitePolicyResolver;
            _crawlerSettings = crawlerSettings;
        }

        public async Task StartAsync()
        {
            await _eventBus.SubscribeAsync<CrawlPageEvent>(_crawlerSettings.ServiceName, EvaluatePageForCrawlingAsync);
            await _eventBus.SubscribeAsync<ScrapePageFailedEvent>(_crawlerSettings.ServiceName, RetryPageCrawlAsync);
        }

        public async Task StopAsync()
        {
            await _eventBus.UnsubscribeAsync<CrawlPageEvent>(_crawlerSettings.ServiceName, EvaluatePageForCrawlingAsync);
            await _eventBus.UnsubscribeAsync<ScrapePageFailedEvent>(_crawlerSettings.ServiceName, RetryPageCrawlAsync);
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
                Service = _crawlerSettings.ServiceName,
                Context = context
            };

            await _eventBus.PublishAsync(clientLogEvent);
        }

        private async Task TestRequestSender(CrawlPageEvent evt)
        {
            var request = evt.CrawlPageRequest;

            // Fetch URL
            var response = await _requestSender.FetchAsync(request.Url,
                request.Options.UserAgent,
                request.Options.UserAccepts);

            // Decode content
            var encoding = response?.Metadata.Encoding;
            var contentAsString = response?.Data?.DecodeAsString(encoding);

            // Output content (first 100 chars)
            var preview = string.IsNullOrWhiteSpace(contentAsString)
                    ? "<no content>"
                    : contentAsString.Substring(0, Math.Min(100, contentAsString.Length));

            _logger.LogDebug("Content: {content} ...", preview);
        }

        /// <summary>
        /// Evaluates whether the page can be crawled based on retry limits, depth,
        /// site policy, and crawler-side rate limiting. If allowed, publishes a scrape event.
        /// </summary>
        public async Task EvaluatePageForCrawlingAsync(CrawlPageEvent evt)
        {
            // Temp Test
            await TestRequestSender(evt);
            return;

            var request = evt.CrawlPageRequest;

            var logMessage = $"Crawl requested: {request.Url} Depth: {request.Depth} Attempt: {request.Attempt}";
            _logger.LogDebug(logMessage);

            if (HasExhaustedRetries(request.Attempt, _crawlerSettings.MaxCrawlAttemptLimit))
            {
                logMessage = $"Crawl Abandoned: {request.Url} Current retry attempt {request.Attempt} exceeded maximum allowed {_crawlerSettings.MaxCrawlAttemptLimit}";
                _logger.LogWarning(logMessage);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Warning,
                    logMessage,
                    "CrawlAbandoned",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Depth = request.Depth,
                        Attempt = request.Attempt
                    });
                return;
            }

            if (HasReachedMaxDepth(request.Depth, request.Options.MaxDepth, _crawlerSettings.MaxCrawlDepthLimit))
            {
                logMessage = $"Crawl Stopped: {request.Url} Current depth {request.Depth} exceeded maximum allowed {request.Options.MaxDepth}.";
                _logger.LogWarning(logMessage);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Warning,
                    logMessage,
                    "CrawlStopped",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Depth = request.Depth,
                        Attempt = request.Attempt
                    });
                return;
            }


            if (!await _sitePolicyResolver.IsPermittedByRobotsTxtAsync(
                request.Url,
                request.Options.UserAgent))
            {
                logMessage = $"Crawl Denied: Robots.txt denied: {request.Url}";
                _logger.LogError(logMessage);

                await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Error,
                    logMessage,
                    "CrawlDenied",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Depth = request.Depth,
                        Attempt = request.Attempt
                    });
                return;
            }


            // Check if the site is currently rate-limited for this request sender's partition.
            var limitedUntil = await _sitePolicyResolver.GetRateLimitAsync(
                request.Url,
                request.Options.UserAgent,
                _requestSender.PartitionKey);

            if (limitedUntil is not null)
            {
                await PublishScheduledCrawlPageEventAsync(request, limitedUntil);
                return;
            }

            await PublishScrapePageEventAsync(evt);
        }




        private async Task RetryPageCrawlAsync(ScrapePageFailedEvent evt)
        {
            var request = evt.CrawlPageRequest;

            var retryAfter = evt.RetryAfter 
                ?? DateTimeOffset.UtcNow.AddSeconds(_crawlerSettings.DefaultRetryDelaySeconds);

            var effectiveRetryAfter = await _sitePolicyResolver.SetRateLimitAsync(
                request.Url,
                request.Options.UserAgent,
                retryAfter,
                evt.PartitionKey);

            await PublishScheduledCrawlPageEventAsync(request, effectiveRetryAfter);
        }


        private static bool HasExhaustedRetries(int currentAttempt, int maxCrawlAttemptLimit) =>
            currentAttempt > maxCrawlAttemptLimit;

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth, int maxCrawlDepthLimit) =>
            currentDepth > Math.Min(maxDepth, maxCrawlDepthLimit);

        private async Task PublishScrapePageEventAsync(CrawlPageEvent evt)
        {
            var request = evt.CrawlPageRequest;

            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageRequest = request,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: request.Depth);

            var logMessage = $"Crawl Permitted: {request.Url} Depth: {request.Depth}";
            _logger.LogInformation(logMessage);

            await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Information,
                    logMessage,
                    "CrawlPermitted",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Depth = request.Depth,
                        Attempt = request.Attempt
                    });
        }

        private async Task PublishScheduledCrawlPageEventAsync(
            CrawlPageRequestDto request, 
            DateTimeOffset? retryAfter)
        {
            var attempt = request.Attempt + 1;
            var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(
                retryAfter,
                _crawlerSettings.ScheduleCrawlDelayMinSeconds,
                _crawlerSettings.ScheduleCrawlDelayMaxSeconds);

            var crawlPageRequest = request with
            {
                Attempt = attempt
            };

            var crawlPageEvent = new CrawlPageEvent
            {
                CrawlPageRequest = crawlPageRequest,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _eventBus.PublishAsync(
                crawlPageEvent, 
                priority: request.Depth, 
                scheduledOffset);

            var logMessage = $"Crawl Deferred: {request.Url} retry scheduled after: {retryAfter?.ToString("o")} Attempt: {attempt}";
            _logger.LogWarning(logMessage);

            await PublishClientLogEventAsync(
                request.GraphId,
                request.CorrelationId,
                LogType.Warning,
                logMessage,
                "CrawlDeferred",
                new LogContext
                {
                    Url = request.Url.AbsoluteUri,
                    Depth = request.Depth,
                    Attempt = attempt
                });
        }
    }
}
