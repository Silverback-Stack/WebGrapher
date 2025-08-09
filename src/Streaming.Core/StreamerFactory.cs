using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Streaming.Core.Adapters.SignalR;

namespace Streaming.Core
{
    public class StreamerFactory
    {
        public static IGraphStreamer Create
            (ILogger logger, 
            IEventBus eventBus,
            IHubContext<GraphStreamerHub> hubContext)
        {
            var service = new SignalRGraphStreamerAdapter(logger, eventBus, hubContext);
            service.SubscribeAll();
            return service;
        }
    }
}
