using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph
{
    public interface IWebGraph
    {
        Task AddWebPageAsync(WebPageItem webPage, Func<Node, Task> onNodePopulated, Func<Node, Task> onLinkDiscovered);
        Task<Node?> GetNodeAsync(int graphId, string url);
        Task<int> TotalPopulatedNodesAsync(int graphId);
        Task CleanupOrphanedNodesAsync(int graphId);

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