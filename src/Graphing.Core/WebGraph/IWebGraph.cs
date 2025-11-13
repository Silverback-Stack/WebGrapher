using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph
{
    public interface IWebGraph
    {
        Task<Graph?> GetGraphAsync(Guid graphId, string userId);

        Task<Graph> CreateGraphAsync(GraphOptions options);

        Task<Graph> UpdateGraphAsync(Graph graph, string userId);

        Task<Graph?> DeleteGraphAsync(Guid graphId, string userId);

        Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize, string userId);


        Task AddWebPageAsync(
            WebPageItem webPage, 
            bool forceRefresh,
            Func<Node, Task> nodePopulatedCallback, 
            Func<Node, Task> linkDiscoveredCallback,
            NodeEdgesUpdateMode linkUpdateMode = NodeEdgesUpdateMode.Append);

        Task<Node?> GetNodeAsync(Guid graphId, string url);

        Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);

        Task<long> TotalPopulatedNodesAsync(Guid graphId);
        
        Task CleanupOrphanedNodesAsync(Guid graphId);

        Task<IEnumerable<Node>> GetInitialGraphNodes(Guid graphId, int topN);

        // IDEAS FOR FUNCTIONS:
        // AverageLinksPerNode()
        // FindReachablePages(string fromUrl, int maxDepth)
        // GetShortestPath(string fromUrl, string toUrl) //Use BFS or Dijkstra
        // GetMostLinkedPages(int topN)
        // GetDeadEnds()
        // SearchByKeyword(string keyword)
        // FindPagesByDomain(string domain)
    }
}