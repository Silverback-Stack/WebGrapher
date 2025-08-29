using System;
using Events.Core.Dtos;

namespace Streaming.Core
{
    public interface IGraphStreamer
    {
        Task StreamGraphPayloadAsync(Guid graphId, SigmaGraphPayloadDto payload);

        Task BroadcastGraphLogAsync(Guid graphId, ClientLogDto payload);

    }
}
