using System;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Events.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Crawler.Core
{
    public class PageCrawler : IPageCrawler, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;
        protected readonly ISitePolicyResolver _sitePolicyResolver;
        protected readonly CrawlerSettings _crawlerSettings;

        public PageCrawler(
            ILogger logger,
            IEventBus eventBus,
            ISitePolicyResolver sitePolicyResolver,
            CrawlerSettings crawlerSettings)
        {
            _eventBus = eventBus;
            _logger = logger;
            _sitePolicyResolver = sitePolicyResolver;
            _crawlerSettings = crawlerSettings;
        }

        public async Task StartAsync()
        {
            await _eventBus.Subscribe<CrawlPageEvent>(_crawlerSettings.ServiceName, EvaluatePageForCrawling);
            await _eventBus.Subscribe<ScrapePageFailedEvent>(_crawlerSettings.ServiceName, RetryPageCrawl);
        }

        public async Task StopAsync()
        {
            await _eventBus.Unsubscribe<CrawlPageEvent>(_crawlerSettings.ServiceName, EvaluatePageForCrawling);
            await _eventBus.Unsubscribe<ScrapePageFailedEvent>(_crawlerSettings.ServiceName, RetryPageCrawl);
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

        /// <summary>
        /// Evaluates whether the page can be crawled based on retry limits, depth,
        /// rate limiting, and robots.txt permissions. If allowed, publishes a scrape event.
        /// </summary>
        public async Task EvaluatePageForCrawling(CrawlPageEvent evt)
        {
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

            var sitePolicy = await _sitePolicyResolver.GetOrCreateSitePolicyAsync(request.Url, request.Options.UserAgent);

            if (!_sitePolicyResolver.IsPermittedByRobotsTxt(request.Url, request.Options.UserAgent, sitePolicy))
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

            if (_sitePolicyResolver.IsRateLimited(sitePolicy))
            {
                await PublishScheduledCrawlPageEventAsync(request, sitePolicy.RetryAfter);
                return;
            }

            await PublishScrapePageEvent(evt);
        }

        private async Task RetryPageCrawl(ScrapePageFailedEvent evt)
        {
            if (evt.RetryAfter is null) return;

            var request = evt.CrawlPageRequest;

            var sitePolicy = await _sitePolicyResolver.GetOrCreateSitePolicyAsync(
                request.Url,
                request.Options.UserAgent,
                evt.RetryAfter); //assigns RetryAfter interval

            await PublishScheduledCrawlPageEventAsync(request, sitePolicy.RetryAfter);
        }

        private static bool HasExhaustedRetries(int currentAttempt, int maxCrawlAttemptLimit) =>
            currentAttempt > maxCrawlAttemptLimit;

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth, int maxCrawlDepthLimit) =>
            currentDepth > Math.Min(maxDepth, maxCrawlDepthLimit);

        private async Task PublishScrapePageEvent(CrawlPageEvent evt)
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

        private async Task PublishScheduledCrawlPageEventAsync(CrawlPageRequestDto request, DateTimeOffset? retryAfter)
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

            var logMessage = $"Crawl Rate-limited: {request.Url} retry scheduled after: {retryAfter?.ToString("o")} Attempt: {attempt}";
            _logger.LogWarning(logMessage);

            await PublishClientLogEventAsync(
                request.GraphId,
                request.CorrelationId,
                LogType.Warning,
                logMessage,
                "CrawlRateLimited",
                new LogContext
                {
                    Url = request.Url.AbsoluteUri,
                    Depth = request.Depth,
                    Attempt = attempt
                });
        }
    }
}
