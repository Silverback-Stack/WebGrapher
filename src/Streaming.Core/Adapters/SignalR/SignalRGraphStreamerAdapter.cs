using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Streaming.Core.Models;

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

        public async override Task StreamNodeAsync(Guid graphId, GraphNode node)
        {
            _logger.LogInformation($"Streaming node {node.Nodes.FirstOrDefault()?.Id} to graph {graphId}");
            
            var clients = _hubContext.Clients.Group(graphId.ToString());

            await clients.SendAsync("ReceiveNode", node);
        }

        public async override Task StreamGraphAsync(Guid graphId, int maxDepth, int maxNodes)
        {
            //TODO: some kind of API call to graphing service to get data
            // GetMostPopularNodesAsync(int graphId, int limit)
            // With each popular node {
            //  TraverseGraphAsync(int graphId, string startUrl, int maxDepth, int? maxNodes = null)
            // }
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
