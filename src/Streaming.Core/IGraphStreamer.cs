using System;
using Streaming.Core.Models;

namespace Streaming.Core
{
    public interface IGraphStreamer
    {
        Task StreamNodeAsync(GraphNode node, int graphId);

        Task StreamGraphAsync(int graphId, int maxDepth, int maxNodes);

        Task BroadcastMessageAsync(string message, int graphId);

        Task BroadcastMetricsAsync(int graphId);

    }
}
