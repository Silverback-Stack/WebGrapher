using Events.Core.Dtos;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core
{
    public interface IPageGrapher
    {
        void SubscribeAll();
        void UnsubscribeAll();

        Task<Graph?> GetGraphByIdAsync(Guid graphId);

        Task<Graph?> CreateGraphAsync(GraphOptions options);

        Task<Graph> UpdateGraphAsync(Graph graph);

        Task<Graph?> DeleteGraphAsync(Guid graphId);

        Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize);

        Task<SigmaGraphPayloadDto> TraverseGraphAsync(Guid graphId, Uri startUrl, int maxDepth, int? maxNodes = null);

        Task<SigmaGraphPayloadDto> PopulateGraphAsync(Guid graphId, int maxDepth, int? maxNodes = null);

        Task CrawlPageAsync(Guid graphId, GraphOptions options);
    }
}