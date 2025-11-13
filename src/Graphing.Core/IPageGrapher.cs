using Events.Core.Dtos;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core
{
    public interface IPageGrapher
    {
        Task StartAsync();
        Task StopAsync();

        Task<Graph?> GetGraphByIdAsync(Guid graphId, string userId);

        Task<Graph?> CreateGraphAsync(GraphOptions options);

        Task<Graph?> UpdateGraphAsync(Graph graph, string userId);

        Task<Graph?> DeleteGraphAsync(Guid graphId, string userId);

        Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize, string userId);

        Task<SigmaGraphPayloadDto> PopulateClientGraphAsync(Guid graphId, int maxDepth, int? maxNodes = null);

        Task<SigmaGraphPayloadDto> GetNodeSubgraphAsync(Guid graphId, Uri nodeUrl, int maxDepth = 1, int? maxNodes = null);

        Task<CrawlPageRequestDto> CrawlPageAsync(Guid graphId, GraphOptions options);
    }
}