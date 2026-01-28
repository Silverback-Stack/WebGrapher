using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Streaming.Infrastructure.Adapters.SignalR
{
    [Authorize]
    public class GraphStreamerHub : Hub
    {
        public async Task JoinGraphGroupAsync(string graphId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, graphId);
        }

    }
}
