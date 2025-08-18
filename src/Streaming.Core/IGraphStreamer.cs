using System;
using Streaming.Core.Models;

namespace Streaming.Core
{
    public interface IGraphStreamer
    {
        Task StreamNodeAsync(Guid graphId, GraphNode node);

        Task StreamGraphAsync(Guid graphId, int maxDepth, int maxNodes);

        Task BroadcastMessageAsync(Guid graphId, string message);

        Task BroadcastMetricsAsync(Guid graphId);

    }
}
