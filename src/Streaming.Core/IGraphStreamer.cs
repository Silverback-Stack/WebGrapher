using System;
using Events.Core.Dtos;

namespace Streaming.Core
{
    public interface IGraphStreamer
    {
        Task StreamGraphPayloadAsync(SigmaGraphPayloadDto payload);

        Task BroadcastMessageAsync(Guid graphId, string message);

        Task BroadcastMetricsAsync(Guid graphId);

    }
}
