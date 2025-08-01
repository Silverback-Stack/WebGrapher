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
            _eventBus.Subscribe<ParsePageEvent>(EventHandler);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<ParsePageEvent>(EventHandler);
        }

        private async Task EventHandler(ParsePageEvent evt)
        {
            var pageItem = Parse(evt.Content);

            if (pageItem != null)
            {
                await _eventBus.PublishAsync(new NormalisePageEvent
                {
                    CrawlPageEvent = evt.CrawlPageEvent,
                    RequestUrl = evt.RequestUrl,
                    ResolvedUrl = evt.ResolvedUrl,
                    Title = pageItem.Title,
                    Keywords = pageItem.Content,
                    Links = pageItem.Links,
                    CreatedAt = DateTimeOffset.UtcNow,
                    StatusCode = evt.StatusCode,
                    LastModified = evt.LastModified
                });

                var linkType = evt.CrawlPageEvent.FollowExternalLinks ? "external" : "internal";
                _logger.LogDebug($"Parsed Page: {evt.ResolvedUrl} found {pageItem.Links.Count()} {linkType} links");
            }
        }

        public abstract PageItem Parse(string content);
    }
}
