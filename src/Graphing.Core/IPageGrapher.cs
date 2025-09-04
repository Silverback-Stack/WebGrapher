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

        Task<SigmaGraphPayloadDto> PopulateGraphAsync(Guid graphId, int maxDepth, int? maxNodes = null);

        Task<SigmaGraphPayloadDto> GetNodeSubgraphAsync(Guid graphId, Uri nodeUrl, int maxDepth = 1, int? maxNodes = null);

        Task<CrawlPageRequestDto> CrawlPageAsync(Guid graphId, GraphOptions options);
    }
}