using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Adapters.SigmaJs;
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
            _eventBus.Unsubscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        private async Task ProcessGraphPageEventAsync(GraphPageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var webPage = MapToWebPage(evt);

            //Delegate : When Link is discovered
            Func<string, Task> onLinkDiscovered = async (url) =>
            {
                var depth = request.Depth + 1;

                var crawlPageEvent = new CrawlPageEvent(
                    new Uri(url),
                    attempt: 1,
                    depth,
                    request);

                var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);

                _logger.LogDebug($"Scheduling crawl for link {url} at depth {depth}");

                await _eventBus.PublishAsync(crawlPageEvent, priority: depth, scheduledOffset);
            };

            //Delegate : When Node is populated with data
            Func<Node, Task> onNodePopulated = async (node) =>
            {
                var sigmaNodes = SigmaJsGraphSerializer.MapNodes(node);
                var sigmaEdges = SigmaJsGraphSerializer.MapEdges(node);

                _logger.LogDebug($"Publishing node populated event for {node.Url}");

                await _eventBus.PublishAsync(new GraphNodeAddedEvent
                {
                    GraphId = node.GraphId,
                    Nodes = sigmaNodes,
                    Edges = sigmaEdges
                });
            };

            await _webGraph.AddWebPageAsync(webPage, onNodePopulated, onLinkDiscovered);
        }

        private WebPageItem MapToWebPage(GraphPageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var result = evt.NormalisePageResult;

            return new WebPageItem
            {
                GraphId = request.GraphId,
                Url = result.Url.AbsoluteUri,
                OriginalUrl = result.OriginalUrl.AbsoluteUri,
                IsRedirect = result.IsRedirect,
                SourceLastModified = result.SourceLastModified,
                Title = result.Title,
                Keywords = result.Keywords,
                Tags = result.Tags,
                Links = result.Links?.Select(l => l.AbsoluteUri) ?? Enumerable.Empty<string>(),
                DetectedLanguageIso3 = result.DetectedLanguageIso3,
            };
        }

    }
}
