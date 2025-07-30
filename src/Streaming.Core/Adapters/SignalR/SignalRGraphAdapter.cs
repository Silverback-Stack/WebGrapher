using System;
using Events.Core.Bus;
using Logging.Core;
using Microsoft.AspNetCore.SignalR;
using Streaming.Core.Models;

namespace Streaming.Core.Adapters.SignalR
{
    public class SignalRGraphAdapter : BaseGraph
    {
        private readonly IHubContext<GraphHub> _hubContext;

        public SignalRGraphAdapter(
            ILogger logger,
            IEventBus eventBus,
            IHubContext<GraphHub> hubContext) : base(logger, eventBus)
        {
            _hubContext = hubContext;
        }


        public async override Task StreamNodeAsync(PageNode node, Guid? graphId = null)
        {
            var dto = PageNode.ToDto(node);

            var clients = graphId is null
                ? _hubContext.Clients.All
                : _hubContext.Clients.Group(graphId.ToString());

            await clients.SendAsync("ReceivePageNode", dto);
        }

        public async override Task StreamEdgeAsync(PageEdge edge, Guid? graphId = null)
        {
            var dto = PageEdge.ToDto(edge);

            var clients = graphId is null
                ? _hubContext.Clients.All
                : _hubContext.Clients.Group(graphId.ToString());

            await clients.SendAsync("ReceivePageEdge", dto);
        }

        public async override Task StreamGraphAsync(Guid? graphId = null)
        {
            //TODO: some kind of API call to get graph data - dont embed the graphing service just to get data connection!
            //var nodes = _graphService.GetAllNodes(); // Your store or in-memory cache
            //var edges = _graphService.GetAllEdges();

            //foreach (var node in nodes)
            //{
            //    await SendNode(node, groupId);
            //}

            //foreach (var edge in edges)
            //{
            //    await SendEdge(edge, groupId);
            //}
        }

        public async override Task BroadcastMessageAsync(string message, Guid? graphId = null)
        {
            throw new NotImplementedException();
        }

        public async override Task BroadcastMetricsAsync(Guid? graphId = null)
        {
            //await Clients.All.SendAsync("ReceiveGraphStats", new
            //{
            //    NodeCount = nodes.Count,
            //    EdgeCount = edges.Count
            //});
        }
    }
}
