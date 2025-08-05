using System;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Events.Core.EventTypes;
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
            _logger.LogInformation($"Crawl requested: {evt.Url} Depth: {evt.Depth} Attempt: {evt.Attempt}");

            if (HasExhaustedRetries(evt.Attempt))
            {
                _logger.LogDebug($"Abandoned crawl: {evt.Url} max retries reached: {evt.Attempt})");
                return;
            }

            if (HasReachedMaxDepth(evt.Depth, evt.MaxDepth))
            {
                _logger.LogDebug($"Stopped crawl: {evt.Url} max depth reached: {evt.Depth})");
                return;
            }

            var sitePolicy = await _sitePolicyResolver.GetOrCreateSitePolicyAsync(evt.Url, evt.UserAgent);

            if (!_sitePolicyResolver.IsPermittedByRobotsTxt(evt.Url, evt.UserAgent, sitePolicy))
            {
                _logger.LogDebug($"Robots.txt denied: {evt.Url} for user agent '{evt.UserAgent}'");
                return;
            }

            if (_sitePolicyResolver.IsRateLimited(sitePolicy))
            {
                _logger.LogDebug($"Rate limited: {evt.Url} retry scheduled after: {sitePolicy.RetryAfter?.ToString("o")} Attempt: {evt.Attempt}");

                await PublishScheduledCrawlPageEvent(evt, sitePolicy.RetryAfter);
                return;
            }

            _logger.LogDebug($"Crawl permitted for {evt.Url}, publishing scrape event. Depth: {evt.Depth}");

            await PublishScrapePageEvent(evt);
        }

        private async Task RetryPageCrawl(ScrapePageFailedEvent evt)
        {
            if (evt.RetryAfter is null) return;

            var sitePolicy = await _sitePolicyResolver.GetOrCreateSitePolicyAsync(
                evt.CrawlPageEvent.Url, 
                evt.CrawlPageEvent.UserAgent, 
                evt.RetryAfter); //assigs RetryAfter interval

            _logger.LogDebug($"Scheduling retry: {evt.CrawlPageEvent.Url} after {sitePolicy.RetryAfter?.ToString("o")}. Next attempt: {evt.CrawlPageEvent.Attempt + 1}");

            await PublishScheduledCrawlPageEvent(evt.CrawlPageEvent, sitePolicy.RetryAfter);
        }

        private static bool HasExhaustedRetries(int currentAttempt) =>
            currentAttempt >= DEFAULT_MAX_CRAWL_ATTEMPTS;

        private static bool HasReachedMaxDepth(int currentDepth, int maxDepth) =>
            currentDepth >= Math.Min(maxDepth, DEFAULT_MAX_CRAWL_DEPTH);

        private async Task PublishScrapePageEvent(CrawlPageEvent evt)
        {
            _logger.LogDebug($"Publishing ScrapePageEvent: {evt.Url} Depth: {evt.Depth} Attempt: {evt.Attempt}");

            await _eventBus.PublishAsync(new ScrapePageEvent
            {
                CrawlPageEvent = evt,
                CreatedAt = DateTimeOffset.UtcNow
            }, priority: evt.Depth);
        }

        private async Task PublishScheduledCrawlPageEvent(CrawlPageEvent evt, DateTimeOffset? retryAfter)
        {
            var attempt = evt.Attempt + 1;
            var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(retryAfter);

            _logger.LogInformation($"Scheduled CrawlPageEvent: {evt.Url} at depth {evt.Depth}, attempt {attempt}, scheduled for {scheduledOffset?.ToString("o")}");

            await _eventBus.PublishAsync(new CrawlPageEvent(
                evt, evt.Url, attempt, evt.Depth), 
                    priority: evt.Depth, scheduledOffset);
        }

    }
}
