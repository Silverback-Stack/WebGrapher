using System;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Helpers;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Crawler.Core
{
    public class PageCrawler : IPageCrawler, IEventBusLifecycle
    {
        protected readonly IEventBus _eventBus;
        protected readonly ILogger _logger;
        protected readonly IRequestSender _requestSender;
        protected readonly ISitePolicyResolver _sitePolicyResolver;

        protected const int DEFAULT_MAX_CRAWL_ATTEMPTS = 3;
        protected const int DEFAULT_MAX_CRAWL_DEPTH = 3;

        public PageCrawler(
            ILogger logger,
            IEventBus eventBus,
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver)
        {
            _eventBus = eventBus;
            _logger = logger;
            _requestSender = requestSender;
            _sitePolicyResolver = sitePolicyResolver;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<CrawlPageEvent>(EvaluatePageForCrawling);
            _eventBus.Subscribe<ScrapePageFailedEvent>(RetryPageCrawl);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<CrawlPageEvent>(EvaluatePageForCrawling);
            _eventBus.Unsubscribe<ScrapePageFailedEvent>(RetryPageCrawl);
        }

        /// <summary>
        /// Evaluates whether the page can be crawled based on retry limits, depth,
        /// rate limiting, and robots.txt permissions. If allowed, publishes a scrape event.
        /// </summary>
        public async Task EvaluatePageForCrawling(CrawlPageEvent evt)
        {
            var request = evt.CrawlPageRequest;

            _logger.LogInformation($"Crawl requested: {request.Url} Depth: {request.Depth} Attempt: {request.Attempt}");

            if (HasExhaustedRetries(request.Attempt))
            {
                _logger.LogDebug($"Abandoned crawl: {request.Url} max retries reached: {request.Attempt})");
                return;
            }

            if (HasReachedMaxDepth(request.Depth, request.MaxDepth))
            {
                _logger.LogDebug($"Stopped crawl: {request.Url} max depth reached: {request.Depth})");
                return;
            }

            var sitePolicy = await _sitePolicyResolver.GetOrCreateSitePolicyAsync(request.Url, request.UserAgent);

            if (!_sitePolicyResolver.IsPermittedByRobotsTxt(request.Url, request.UserAgent, sitePolicy))
            {
                _logger.LogDebug($"Robots.txt denied: {request.Url} for user agent '{request.UserAgent}'");
                return;
            }

            if (_sitePolicyResolver.IsRateLimited(sitePolicy))
            {
                _logger.LogDebug($"Rate limited: {request.Url} retry scheduled after: {sitePolicy.RetryAfter?.ToString("o")} Attempt: {request.Attempt}");

                await PublishScheduledCrawlPageEvent(request, sitePolicy.RetryAfter);
                return;
            }

            _logger.LogDebug($"Crawl permitted for {request.Url}, publishing scrape event. Depth: {request.Depth}");

            await PublishScrapePageEvent(evt);
        }

        private async Task RetryPageCrawl(ScrapePageFailedEvent evt)
        {
            if (evt.RetryAfter is null) return;

            var request = evt.CrawlPageRequest;

            var sitePolicy = await _sitePolicyResolver.GetOrCreateSitePolicyAsync(
                request.Url,
                request.UserAgent,
                evt.RetryAfter); //assigns RetryAfter interval

            _logger.LogDebug($"Scheduling retry: {request.Url} after {sitePolicy.RetryAfter?.ToString("o")}. Next attempt: {request.Attempt + 1}");

            await PublishScheduledCrawlPageEvent(request, sitePolicy.RetryAfter);
        }

        private static bool HasExhaustedRetries(int currentAttempt) =>
            currentAttempt >= DEFAULT_MAX_CRAWL_ATTEMPTS;

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);

        private async Task PublishScrapePageEvent(CrawlPageEvent evt)
        {
            var request = evt.CrawlPageRequest;

            _logger.LogDebug($"Publishing ScrapePageEvent: {request.Url} Depth: {request.Depth} Attempt: {request.Attempt}");

            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageRequest = request,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: request.Depth);
        }

        private async Task PublishScheduledCrawlPageEvent(CrawlPageRequestDto request, DateTimeOffset? retryAfter)
        {
            var attempt = request.Attempt + 1;
            var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(retryAfter);

            _logger.LogInformation($"Scheduled CrawlPageEvent: {request.Url} at depth {request.Depth}, attempt {attempt}, scheduled for {scheduledOffset?.ToString("o")}");

            await _eventBus.PublishAsync(
                new CrawlPageEvent(
                    request.Url, 
                    attempt, 
                    request.Depth, 
                    request), 
                priority: request.Depth, 
                scheduledOffset);
        }

    }
}
