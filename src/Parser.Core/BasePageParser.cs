using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Microsoft.Extensions.Logging;

namespace Parser.Core
{
    public abstract class BasePageParser : IPageParser, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

        public BasePageParser(ILogger logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<ParsePageEvent>(EvaluatePageAsync);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<ParsePageEvent>(EvaluatePageAsync);
        }

        private async Task EvaluatePageAsync(ParsePageEvent evt)
        {
            var pageItem = Parse(evt.Content);

            if (pageItem is null)
            {
                _logger.LogDebug($"Parsing failed for {evt.Url} Attempt {evt.CrawlPageEvent.Attempt} - No content returned.");
                return;
            }

            var linkType = evt.CrawlPageEvent.FollowExternalLinks ? "external" : "internal";
            _logger.LogDebug($"Parsed page {evt.Url} Attempt {evt.CrawlPageEvent.Attempt} - Found {pageItem.Links.Count()} {linkType} links.");

            await PublishNormalisationEventAsync(evt, pageItem);
        }

        private async Task PublishNormalisationEventAsync(ParsePageEvent evt, PageItem pageItem)
        {
            _logger.LogTrace($"Publishing NormalisePageEvent for {evt.Url} with {pageItem.Links.Count()} links at depth {evt.CrawlPageEvent.Depth}");

            await _eventBus.PublishAsync(new NormalisePageEvent
            {
                CrawlPageEvent = evt.CrawlPageEvent,
                Url = evt.Url,
                Title = pageItem.Title,
                Keywords = pageItem.Content,
                Links = pageItem.Links,
                CreatedAt = DateTimeOffset.UtcNow,
                StatusCode = evt.StatusCode,
                LastModified = evt.LastModified
            }, priority: evt.CrawlPageEvent.Depth);
        }

        public abstract PageItem Parse(string content);
    }
}
