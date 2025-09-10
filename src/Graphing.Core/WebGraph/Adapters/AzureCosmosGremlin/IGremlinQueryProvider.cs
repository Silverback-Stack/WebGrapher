using System;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin
{
    public interface IGremlinQueryProvider
    {
        // Graph queries
        Task<dynamic?> GetGraphVertexAsync(Guid graphId);
        Task<IEnumerable<dynamic>> ListGraphVerticesAsync(int start, int end);
        Task<int> CountGraphVerticesAsync();
        Task<dynamic?> CreateGraphVertexAsync(Graph graph);
        Task UpdateGraphVertexAsync(Graph graph);
        Task DeleteGraphVertexAsync(Guid graphId);


        // Node queries
        Task<dynamic?> GetNodeVertexAsync(Guid graphId, string url);

        Task<IEnumerable<(dynamic source, dynamic target, bool isOutgoing)>> 
            GetNodeVerticesEdgesAsync(Guid graphId, IEnumerable<string> urls);

        Task UpsertNodeVertexAsync(Node node);

        Task AddNodeVertexEdgeAsync(Node fromNode, Node toNode, Guid graphId);

        Task RemoveNodeVertexEdgesAsync(Guid graphId, Node node);
        Task RemoveOrphanedNodeVerticesAsync(Guid graphId);

        Task<int> CountNodeVertexEdgesAsync(Guid graphId, Node node);

        Task<IEnumerable<dynamic>> GetNodeVerticesByCreationDateAscAsync(Guid graphId, int topN);

        Task<long> CountNodeVerticesPopulatedAsync(Guid graphId);

        Task<IEnumerable<dynamic>> GetNodeVertexSubgraphAsync(Guid graphId, string vertexId, int maxDepth, int? maxNodes = null);
    }
}
