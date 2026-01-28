using Events.Core.Bus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Streaming.Core;
using Streaming.Infrastructure.Adapters.SignalR;
using Streaming.Infrastructure.Adapters.SignalRServerless;
using System;

namespace Streaming.Factories
{
    public class StreamingFactory
    {
        public static IGraphStreamer Create
            (ILogger logger, 
            IEventBus eventBus,
            IHubContext<GraphStreamerHub>? hubContext,
            StreamingConfig streamingConfig)
        {
            switch (streamingConfig.Provider)
            {
                case StreamingProvider.SignalRAzureServerless:
                    return new SignalRServerlessGraphStreamerAdapter(logger, eventBus, streamingConfig.Settings);

                default:
                    return new SignalRGraphStreamerAdapter(logger, eventBus, hubContext!, streamingConfig.Settings);
            }
        }
    }
}
