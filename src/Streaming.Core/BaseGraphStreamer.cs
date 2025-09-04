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
        protected readonly StreamingSettings _streamingSettings;

        public BaseGraphStreamer(
            ILogger logger, 
            IEventBus eventBus,
            StreamingSettings streamingSettings)
        {
            _logger = logger;
            _eventBus = eventBus;
            _streamingSettings = streamingSettings;
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
            Guid? correlationId,
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
                Service = _streamingSettings.ServiceName,
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

                var logMessage = $"Streaming {payload.NodeCount} nodes and {payload.EdgeCount} edges.";
                _logger.LogInformation(logMessage);

                await PublishClientLogEventAsync(
                        payload.GraphId,
                        payload.CorrolationId,
                        LogType.Information,
                        logMessage,
                        "StreamingPayload",
                        new LogContext
                        {
                            NodeCount = payload.NodeCount,
                            EdgeCount = payload.EdgeCount,
                            Nodes = payload.Nodes.Select(n => n.Id)
                        });
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
