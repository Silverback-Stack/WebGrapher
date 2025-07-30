using System;
using Streaming.Core.Models;

namespace Streaming.Core
{
    public interface IGraphStreamer
    {
        /// <summary>
        /// Sends a graph node update to connected clients.
        /// </summary>
        /// <param name="node">The graph node to stream.</param>
        Task StreamNodeAsync(PageNode node, Guid? graphId = null);

        /// <summary>
        /// Sends a graph edge update to connected clients.
        /// </summary>
        /// <param name="edge">The graph edge to stream.</param>
        Task StreamEdgeAsync(PageEdge edge, Guid? graphId = null);

        /// <summary>
        /// Broadcasts a full graph snapshot for visualization.
        /// </summary>
        /// <param name="graph">The complete graph model.</param>
        Task StreamGraphAsync(Guid? graphId = null);

        /// <summary>
        /// Pushes a client notification or meta message.
        /// </summary>
        /// <param name="message">A textual or metadata message.</param>
        Task BroadcastMessageAsync(string message, Guid? graphId = null);

        Task BroadcastMetricsAsync(Guid? graphId = null);

    }
}
