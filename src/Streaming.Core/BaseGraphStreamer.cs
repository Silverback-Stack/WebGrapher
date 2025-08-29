using System;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Microsoft.Extensions.Logging;

namespace Streaming.Core
{
    public abstract class BaseGraphStreamer : IGraphStreamer, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;

        protected const string SERVICE_NAME = "STREAMING";

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
            _eventBus.Subscribe<ClientLogEvent>(ProcessGraphLogEvent);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<GraphNodeAddedEvent>(ProcessGraphNodeAddedEvent);
            _eventBus.Unsubscribe<ClientLogEvent>(ProcessGraphLogEvent);
        }

        public async Task PublishClientLogEventAsync(
            Guid graphId, 
            Guid correlationId,
            LogType type, 
            string message, 
            string? code = null, 
            Object? context = null)
        {
            var clientLogEvent = new ClientLogEvent
            {
                GraphId = graphId,
                CorrelationId = correlationId,
                Type = type,
                Message = message,
                Code = code,
                Service = SERVICE_NAME,
                Context = context
            };

            await _eventBus.PublishAsync(clientLogEvent);
        }

        public abstract Task StreamGraphPayloadAsync(Guid graphId, SigmaGraphPayloadDto payload);

        public abstract Task BroadcastGraphLogAsync(Guid graphId, ClientLogDto payload);

        private async Task ProcessGraphNodeAddedEvent(GraphNodeAddedEvent evt)
        {
            try
            {
                var payload = evt.SigmaGraphPayload;
                await StreamGraphPayloadAsync(evt.SigmaGraphPayload.GraphId, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Streaming Failed: Failed to stream node to GraphId: {evt.SigmaGraphPayload.GraphId}");
            }
        }

        private async Task ProcessGraphLogEvent(ClientLogEvent evt)
        {
            var clientLogDto = new ClientLogDto
            {
                Id = evt.Id,
                GraphId = evt.GraphId,
                CorrelationId = evt.CorrelationId,
                Type = evt.Type.ToString(),
                Message = evt.Message,
                Code = evt.Code,
                Service = evt.Service,
                Timestamp = evt.Timestamp,
                Context = evt.Context
            };

            try
            {
                await BroadcastGraphLogAsync(evt.GraphId, clientLogDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Streaming Failed: Failed to stream log to GraphId: {clientLogDto.GraphId}");
            }
        }

    }
}
