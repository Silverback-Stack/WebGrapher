using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core
{
    public class PageGrapher : IPageGrapher, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IWebGraph _webGraph;

        public PageGrapher(ILogger logger, IEventBus eventBus, IWebGraph webGraph)
        {
            _logger = logger;
            _eventBus = eventBus;
            _webGraph = webGraph;
        }
        public void SubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        private async Task ProcessGraphPageEventAsync(GraphPageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var webPage = Map(evt);

            //Create delegate:
            Action<string> onLinkDiscovered = (url) =>
            {
                var depth = request.Depth + 1;

                var crawlPageEvent = new CrawlPageEvent(
                    new Uri(url),
                    attempt: 1,
                    depth,
                    request);

                var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);

                _logger.LogDebug($"Scheduling crawl for link {url} at depth {depth}");

                _eventBus.PublishAsync(crawlPageEvent, priority: depth, scheduledOffset);
            };

            await _webGraph.AddWebPageAsync(webPage, onLinkDiscovered);
        }

        private WebPageItem Map(GraphPageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var result = evt.NormalisePageResult;

            return new WebPageItem
            {
                Url = result.Url.AbsoluteUri,
                OriginalUrl = result.OriginalUrl.AbsoluteUri,
                GraphId = request.GraphId,
                DetectedLanguageIso3 = result.DetectedLanguageIso3,
                IsRedirect = result.IsRedirect,
                Links = result.Links != null ? result.Links.Select(l => l.AbsoluteUri) : new List<string>(),
                SourceLastModified = result.SourceLastModified
            };
        }

    }
}
