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
            IHubContext<GraphStreamerHub> hubContext, 
            StreamingSettings streamingSettings)
        {
            var service = new SignalRGraphStreamerAdapter(logger, eventBus, hubContext, streamingSettings);

            return service;
        }
    }
}
