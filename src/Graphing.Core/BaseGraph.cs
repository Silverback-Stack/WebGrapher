using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Graphing.Core.Models;
using Microsoft.Extensions.Logging;

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
                        evt.Links.Select(u => u.AbsoluteUri));

            await FollowEdges(evt, node);
        }

        private async Task FollowEdges(GraphPageEvent evt, Node node)
        {
            if (evt.CrawlPageEvent.Depth > 1)
            {
                return;
            };

            foreach (var edge in node.Edges)
            {
                var depth = evt.CrawlPageEvent.Depth + 1;

                _logger.LogWarning($"Processing {edge} depth {depth}");

                if (!IsNodePopulated(edge))
                {
                    var crawlPageEvent = new CrawlPageEvent(
                        evt.CrawlPageEvent,
                        new Uri(edge),
                        attempt: 1,
                        depth);

                    var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);
                    await _eventBus.PublishAsync(crawlPageEvent, scheduledOffset);
                } else
                {
                    _logger.LogWarning($"Skipping {edge} depth {depth} as already processed");
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
