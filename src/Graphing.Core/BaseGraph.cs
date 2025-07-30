using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Graphing.Core.Models;
using Logging.Core;

namespace Graphing.Core
{
    public abstract class BaseGraph : IGraph, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;

        protected BaseGraph(ILogger logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(EventHandler);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(EventHandler);
        }

        private async Task EventHandler(GraphPageEvent evt)
        {
            //if (_graphAnalyser.TotalNodes() > 1000 && _graphAnalyser.TotalEdges() > 1000)
            //{
            //    var gexfExporter = new GexfGraphExporter();
            //    var data = gexfExporter.Export(_nodes);
            //}

            var node = AddNode(
                        evt.Url.AbsoluteUri,
                        evt.Title,
                        evt.Keywords,
                        evt.SourceLastModified,
                        evt.Links);

            await FollowEdges(evt, node);
        }

        private async Task FollowEdges(GraphPageEvent evt, Node node)
        {
            foreach (var edge in node.Edges)
            {
                if (!Uri.TryCreate(edge, UriKind.Absolute, out var edgeUri))
                    continue;

                if (!IsNodePopulated(edge))
                {
                    var depth = evt.CrawlPageEvent.Depth + 1;

                    var crawlPageEvent = new CrawlPageEvent(
                        evt.CrawlPageEvent,
                        edgeUri,
                        attempt: 1,
                        depth);

                    var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);
                    await _eventBus.PublishAsync(crawlPageEvent, scheduledOffset);
                }
            }
        }

        public abstract IGraphAnalyser GraphAnalyser { get; }

        public abstract Node AddNode(string id, string title, string keywords, DateTimeOffset? sourceLastModified, IEnumerable<string> edges);

        public abstract bool IsNodePopulated(string id);

        public abstract void RemoveNode(string id);

        public abstract void Dispose();
    }
}
