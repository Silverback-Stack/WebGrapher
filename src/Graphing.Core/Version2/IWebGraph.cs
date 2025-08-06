
namespace Graphing.Core.Version2
{
    public interface IWebGraph
    {
        Task AddWebPageAsync(WebPageItem page, Action<string> onLinkDiscovered)
        Task<WebGraphNode?> GetNodeAsync(int graphId, string url);
        Task<int> TotalPopulatedNodesAsync(int graphId);
        Task CleanupOrphanedNodesAsync(int graphId);
    }
}