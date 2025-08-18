using System;
using Events.Core.Bus;
using Events.Core.Events;
using Microsoft.Extensions.Logging;
using Streaming.Core.Models;

namespace Streaming.Core
{
    public abstract class BaseGraphStreamer : IGraphStreamer, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;

        public BaseGraphStreamer(
            ILogger logger, 
            IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<GraphNodeAddedEvent>(ProcessGraphNodeAddedEvent);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<GraphNodeAddedEvent>(ProcessGraphNodeAddedEvent);
        }

        public abstract Task StreamNodeAsync(Guid graphId, GraphNode node);

        public abstract Task StreamGraphAsync(Guid graphId, int maxDepth, int maxNodes);

        public abstract Task BroadcastMessageAsync(Guid graphId, string message);

        public abstract Task BroadcastMetricsAsync(Guid graphId);

        private async Task ProcessGraphNodeAddedEvent(GraphNodeAddedEvent evt)
        {
            try
            {
                var node = new GraphNode
                {
                    Nodes = evt.Nodes,
                    Edges = evt.Edges
                };

                await BroadcastMessageAsync(evt.GraphId, "Sending node...");
                await StreamNodeAsync(evt.GraphId, node);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process GraphNodeAddedEvent for graph {evt.GraphId}");
            }
        }
    }
}
