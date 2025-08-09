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

        public async override Task StreamNodeAsync(WebNode webNode, int graphId)
        {
            _logger.LogInformation($"Streaming node {webNode.Nodes.FirstOrDefault()?.Id} to graph {graphId}");
            
            var clients = _hubContext.Clients.Group(graphId.ToString());

            await clients.SendAsync("ReceiveWebNode", webNode);
        }

        public async override Task StreamGraphAsync(int graphId, int maxDepth, int maxNodes)
        {
            //TODO: some kind of API call to graphing service to get data
            // GetMostPopularNodesAsync(int graphId, int limit)
            // With each popular node {
            //  TraverseGraphAsync(int graphId, string startUrl, int maxDepth, int? maxNodes = null)
            // }
        }

        public async override Task BroadcastMessageAsync(string message, int graphId)
        {
            var clients = _hubContext.Clients.Group(graphId.ToString());
            await clients.SendAsync("ReceiveMessage", message);
        }

        public async override Task BroadcastMetricsAsync(int graphId)
        {
            //TODO: some kind of API call to graphing service to get data
            // TotalNodesAsync
            // TotalPopulatedNodesAsync
            // etc
        }
    }
}
