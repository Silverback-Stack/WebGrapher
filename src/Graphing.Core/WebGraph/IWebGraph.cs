using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph
{
    public interface IWebGraph
    {
        Task AddWebPageAsync(
            WebPageItem webPage, 
            Func<Node, Task> onNodePopulated, 
            Func<Node, Task> onLinkDiscovered,
            LinkUpdateMode linkUpdateMode = LinkUpdateMode.Append);

        Task<Graph?> GetGraphAsync(Guid graphId);

        Task<Graph> CreateGraphAsync(
            string name, 
            string description,
            Uri url,
            int maxDepth,
            int maxLinks,
            bool excludeExternalLinks,
            bool excludeQueryStrings,
            string urlMatchRegex,
            string titleElementXPath,
            string contentElementXPath,
            string summaryElementXPath,
            string imageElementXPath,
            string relatedLinksElementXPath);

        Task<Graph> UpdateGraphAsync(Graph graph);

        Task<Graph?> DeleteGraphAsync(Guid graphId);


        Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize);

        Task<Node?> GetNodeAsync(Guid graphId, string url);

        Task<IEnumerable<Node>> TraverseGraphAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);

        Task<int> TotalPopulatedNodesAsync(Guid graphId);
        
        Task CleanupOrphanedNodesAsync(Guid graphId);

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