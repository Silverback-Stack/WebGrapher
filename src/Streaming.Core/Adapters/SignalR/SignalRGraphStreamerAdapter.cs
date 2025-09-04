using System;
using Events.Core.Bus;
using Events.Core.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Streaming.Core.Adapters.SignalR
{
    public class SignalRGraphStreamerAdapter : BaseGraphStreamer
    {
        private readonly IHubContext<GraphStreamerHub> _hubContext;

        public SignalRGraphStreamerAdapter(
            ILogger logger,
            IEventBus eventBus,
            IHubContext<GraphStreamerHub> hubContext,
            StreamingSettings streamingSettings) : base(logger, eventBus, streamingSettings)
        {
            _hubContext = hubContext;
        }

        public async override Task StreamGraphPayloadAsync(Guid graphId, SigmaGraphPayloadDto payload)
        {
            var firstNodeId = payload.Nodes.FirstOrDefault()?.Id ?? "N/A";
            _logger.LogDebug($"Streaming payload starting with node {firstNodeId} to graph {graphId}");

            var clients = _hubContext.Clients.Group(graphId.ToString());
            await clients.SendAsync("ReceiveGraphPayload", payload);
        }

        public async override Task BroadcastGraphLogAsync(Guid graphId, ClientLogDto payload)
        {
            _logger.LogDebug($"Streaming client log to graph {graphId}");

            var clients = _hubContext.Clients.Group(graphId.ToString());
            await clients.SendAsync("ReceiveGraphLog", payload);
        }

    }
}
