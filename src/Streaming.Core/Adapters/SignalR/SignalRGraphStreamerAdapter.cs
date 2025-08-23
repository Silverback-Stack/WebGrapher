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
            IHubContext<GraphStreamerHub> hubContext) : base(logger, eventBus)
        {
            _hubContext = hubContext;
        }

        public async override Task StreamGraphPayloadAsync(SigmaGraphPayloadDto payload)
        {
            var firstNodeId = payload.Nodes.FirstOrDefault()?.Id ?? "N/A";
            _logger.LogInformation($"Streaming payload starting with node {firstNodeId} to graph {payload.GraphId}");

            var clients = _hubContext.Clients.Group(payload.GraphId.ToString());
            await clients.SendAsync("ReceiveGraphPayload", payload);
        }

        public async override Task BroadcastMessageAsync(Guid graphId, string message)
        {
            var clients = _hubContext.Clients.Group(graphId.ToString());
            await clients.SendAsync("ReceiveMessage", message);
        }

        public async override Task BroadcastMetricsAsync(Guid graphId)
        {
            //TODO: some kind of API call to graphing service to get data
            // TotalNodesAsync
            // TotalPopulatedNodesAsync
            // etc
        }
    }
}
