using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph
{
    public interface IWebGraph
    {
        Task AddWebPageAsync(
            WebPageItem webPage, 
            bool forceRefresh,
            Func<Node, Task> nodePopulatedCallback, 
            Func<Node, Task> linkDiscoveredCallback,
            LinkUpdateMode linkUpdateMode = LinkUpdateMode.Append);

        Task<Graph?> GetGraphByIdAsync(Guid graphId);

        Task<Graph> CreateGraphAsync(GraphOptions options);

        Task<Graph> UpdateGraphAsync(Graph graph);

        Task<Graph?> DeleteGraphAsync(Guid graphId);


        Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize);

        Task<Node?> GetNodeAsync(Guid graphId, string url);

        Task<IEnumerable<Node>> TraverseGraphAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);

        Task<int> TotalPopulatedNodesAsync(Guid graphId);
        
        Task CleanupOrphanedNodesAsync(Guid graphId);

        Task<IEnumerable<Node>> GetMostPopularNodes(Guid graphId, int topN);

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