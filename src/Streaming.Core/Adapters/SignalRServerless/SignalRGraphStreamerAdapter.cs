using Events.Core.Bus;
using Events.Core.Dtos;
using Microsoft.Extensions.Logging;
using System;

namespace Streaming.Core.Adapters.SignalRServerless
{
    public class SignalRServerlessGraphStreamerAdapter : BaseGraphStreamer
    {
        public SignalRServerlessGraphStreamerAdapter(
            ILogger logger, 
            IEventBus eventBus, 
            StreamingSettings streamingSettings) 
            : base(logger, eventBus, streamingSettings)
        {
        }

        public override Task BroadcastGraphLogAsync(Guid graphId, ClientLogDto payload)
        {
            throw new NotSupportedException("Azure SignalR Serverless mode is currently not supported.");
        }

        public override Task StreamGraphPayloadAsync(Guid graphId, SigmaGraphPayloadDto payload)
        {
            throw new NotSupportedException("Azure SignalR Serverless mode is currently not supported.");
        }

    }
}
