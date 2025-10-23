using Events.Core.Bus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Streaming.Core.Adapters.SignalR;
using Streaming.Core.Adapters.SignalRServerless;
using System;

namespace Streaming.Core
{
    public class StreamerFactory
    {
        public static IGraphStreamer Create
            (ILogger logger, 
            IEventBus eventBus,
            IHubContext<GraphStreamerHub>? hubContext, 
            StreamingSettings streamingSettings)
        {
            if (hubContext == null)
            {
                return new SignalRServerlessGraphStreamerAdapter(logger, eventBus, streamingSettings);
            }
            else
            {
                return new SignalRGraphStreamerAdapter(logger, eventBus, hubContext, streamingSettings);
            }
        }
    }
}
