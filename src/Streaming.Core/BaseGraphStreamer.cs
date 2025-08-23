using System;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Microsoft.Extensions.Logging;

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

        public abstract Task StreamGraphPayloadAsync(SigmaGraphPayloadDto payload);

        public abstract Task BroadcastMessageAsync(Guid graphId, string message);

        public abstract Task BroadcastMetricsAsync(Guid graphId);

        private async Task ProcessGraphNodeAddedEvent(GraphNodeAddedEvent evt)
        {
            try
            {
                var payload = evt.SigmaGraphPayload;

                await BroadcastMessageAsync(payload.GraphId, "Sending node(s)...");
                await StreamGraphPayloadAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process GraphNodeAddedEvent for graph {evt.SigmaGraphPayload.GraphId}");
            }
        }
    }
}
