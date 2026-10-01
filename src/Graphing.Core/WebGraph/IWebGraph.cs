using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph
{
    public interface IWebGraph
    {
        Task<Graph?> GetGraphAsync(Guid graphId, string userId);

        Task<Graph> CreateGraphAsync(Guid graphId, string userId, GraphOptions options);

        Task EnsureGraphExistsAsync(Guid graphId, string userId, GraphOptions options);

        Task MapPageAsync(
            Guid graphId,
            PageData pageData,
            int crawlDepth,
            Func<Node, Task> nodePopulatedCallback,
            Func<Node, Task> nodePopulationRequestCallback);

        Task<string> GetGraphDiagnosticViewAsync(
            Guid graphId,
            int maxDepth,
            int? maxNodes);






        Task<Graph> UpdateGraphAsync(Graph graph, string userId);

        Task<Graph?> DeleteGraphAsync(Guid graphId, string userId);

        Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize, string userId);

        Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);
       
        Task<IEnumerable<Node>> GetInitialGraphNodes(Guid graphId, int topN);


        // DO NOT INCLUDE IN DEMO - WORKING BUT NOT CURRENTLY BEING USED
        Task CleanupOrphanedNodesAsync(Guid graphId);

        // FUTURE IDEAS FOR FUNCTIONS:
        // AverageLinksPerNode()
        // FindReachablePages(string fromUrl, int maxDepth)
        // GetShortestPath(string fromUrl, string toUrl) //Use BFS or Dijkstra
        // GetMostLinkedPages(int topN)
        // GetDeadEnds()
        // SearchByKeyword(string keyword)
        // FindPagesByDomain(string domain)
    }
}