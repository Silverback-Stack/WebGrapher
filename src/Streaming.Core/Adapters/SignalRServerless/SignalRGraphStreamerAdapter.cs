using System;
using Events.Core.Bus;
using Events.Core.Dtos;
using Microsoft.Extensions.Logging;

namespace Streaming.Core.Adapters.SignalRServerless
{
    public class SignalRServerlessGraphStreamerAdapter : BaseGraphStreamer
    {
        //TODO: Implement this adapter for Azure SignalR Serverless mode

        public SignalRServerlessGraphStreamerAdapter(ILogger logger, IEventBus eventBus, StreamingSettings streamingSettings) : base(logger, eventBus, streamingSettings)
        {
        }

        public override Task BroadcastGraphLogAsync(Guid graphId, ClientLogDto payload)
        {
            throw new NotImplementedException();
        }

        public override Task StreamGraphPayloadAsync(Guid graphId, SigmaGraphPayloadDto payload)
        {
            throw new NotImplementedException();
        }
    }
}
