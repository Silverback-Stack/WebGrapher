
using Microsoft.AspNetCore.SignalR;

namespace Streaming.Core.Adapters.SignalR
{
    public class GraphStreamerHub : Hub
    {
        public async Task JoinGraphGroup(string graphId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, graphId);
        }

    }
}
