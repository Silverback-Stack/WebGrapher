using Auth.WebApi;
using Graphing.Core;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Dtos;
using Graphing.Core.WebGraph.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Graph = Graphing.Core.WebGraph.Models.Graph;

namespace Graphing.WebApi.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GraphController : ControllerBase
    {
        private readonly IPageGrapher _pageGrapher;
        private readonly UserContext _userContext;

        public GraphController(
            IPageGrapher pageGrapher, 
            UserContext userContext)
        {
            _pageGrapher = pageGrapher;
            _userContext = userContext;
        }



        [HttpPost("{graphId}/crawl", Name = "Crawl")]
        public async Task<IActionResult> CrawlPageAsync([FromRoute] Guid graphId, [FromBody] CrawlPageDto crawlPage)
        {
            if (crawlPage == null)
                return BadRequest("Request body cannot be empty.");

            if (crawlPage.CrawlRequest == null)
                return BadRequest("Crawl request cannot be empty.");

            if (!Uri.TryCreate(
                crawlPage.CrawlRequest.Url,
                UriKind.Absolute,
                out _))
            {
                ModelState.AddModelError(
                    nameof(CrawlRequestDto.Url),
                    "Invalid URL. Use format https://www.example.com");

                return ValidationProblem();
            }

            // Map crawl request parameters
            var crawlRequest = MapToModel(crawlPage.CrawlRequest);


            var existingGraph = await _pageGrapher.GetGraphByIdAsync(
                graphId,
                _userContext.UserId);

            if (existingGraph == null)
                return NotFound();

            if (crawlPage.OverwriteDefaults && !crawlPage.Preview)
            {
                existingGraph.CrawlRequest = crawlRequest;

                var updatedGraph = await _pageGrapher.UpdateGraphAsync(
                    existingGraph,
                    _userContext.UserId);

                if (updatedGraph == null)
                    return StatusCode(500, "Failed to update graph.");
            }

            // Submit crawl page request
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            var crawlPageRequestDto = await _pageGrapher.CrawlPageAsync(
                graphId,
                new GraphOptions
                {
                    CrawlRequest = crawlRequest,

                    UserAgent = string.IsNullOrWhiteSpace(userAgent)
                        ? GraphOptions.DefaultUserAgent
                        : userAgent
                },
                crawlPage.Preview);

            return Ok(crawlPageRequestDto);
        }



        // GET existing graph
        [HttpGet("{graphId}", Name = "GetById")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] Guid graphId)
        {
            var graph = await _pageGrapher.GetGraphByIdAsync(graphId, _userContext.UserId);

            if (graph == null) 
                return NotFound();

            var graphDto = MapToDto(graph);

            return Ok(graphDto);
        }



        // POST create new graph
        [HttpPost("create", Name = "Create")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateGraphDto createGraph)
        {
            try
            {
                if (createGraph == null)
                    return BadRequest("Request body cannot be empty.");

                var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

                var userId = _userContext.UserId; //assign graph owner from user context

                var newGraph = await _pageGrapher.CreateGraphAsync(
                    userId,
                    new GraphOptions
                    {
                        Name = createGraph.Name,
                        Description = createGraph.Description,
                        CrawlRequest = new GraphCrawlRequest(),
                        UserAgent = string.IsNullOrWhiteSpace(userAgent)
                            ? GraphOptions.DefaultUserAgent
                            : userAgent
                    });

                var createdDto = MapToDto(newGraph);

                // Return 201 Created with route to the new graph
                return CreatedAtRoute(
                    "GetById",
                    new { graphId = newGraph.Id },
                    createdDto
                );
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to create graph.");
            }
        }



        [HttpPut("{graphId}/update", Name = "Update")]
        public async Task<IActionResult> UpdateGraphAsync(
            [FromRoute] Guid graphId,
            [FromBody] UpdateGraphDto updateGraph)
        {
            try
            {
                if (updateGraph == null)
                    return BadRequest("Request body cannot be empty.");

                if (updateGraph.CrawlRequest == null)
                    return BadRequest("Crawl request cannot be empty.");

                // Validate URL if provided
                if (!string.IsNullOrWhiteSpace(updateGraph.CrawlRequest.Url) &&
                    !Uri.TryCreate(
                        updateGraph.CrawlRequest.Url,
                        UriKind.Absolute,
                        out _))
                {
                    return BadRequest("URL format is invalid.");
                }

                var existingGraph = await _pageGrapher.GetGraphByIdAsync(
                    graphId,
                    _userContext.UserId);

                if (existingGraph == null)
                    return NotFound();

                existingGraph.Name = updateGraph.Name;
                existingGraph.Description = updateGraph.Description;
                existingGraph.CrawlRequest = MapToModel(updateGraph.CrawlRequest);

                var updatedGraph = await _pageGrapher.UpdateGraphAsync(
                    existingGraph,
                    _userContext.UserId);

                return Ok(MapToDto(updatedGraph));
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to update graph.");
            }
        }




        [HttpDelete("{graphId}/delete", Name = "Delete")]
        public async Task<IActionResult> DeleteGraphAsync([FromRoute] Guid graphId)
        {
            var deletedGraph = await _pageGrapher.DeleteGraphAsync(graphId, _userContext.UserId);

            if (deletedGraph == null)
                return NotFound();

            // Successfully deleted
            return NoContent();
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListGraphsAsync(int page = 1, int pageSize = 8)
        {
            if (pageSize > 20) pageSize = 20;

            var result = await _pageGrapher.ListGraphsAsync(page, pageSize, _userContext.UserId);

            var dtoResult = new PagedResult<ListGraphDto>(
                result.Items.Select(g => new ListGraphDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CreatedAt = g.CreatedAt,
                    Url = g.CrawlRequest.Url?.AbsoluteUri ?? string.Empty
                }),
                result.TotalCount,
                result.Page,
                result.PageSize
            );

            return Ok(dtoResult);
        }




        [HttpGet("{graphId}/populate", Name = "Populate")]
        public async Task<IActionResult> PopulateClientGraphAsync([FromRoute] Guid graphId, [FromQuery] int maxDepth, [FromQuery] int? maxNodes = null)
        {
            var data = await _pageGrapher.PopulateClientGraphAsync(graphId, maxDepth, maxNodes);

            if (data == null || !data.Nodes.Any())
            {
                return NotFound(new { message = $"No data found for graph {graphId}" });
            }

            return Ok(data);
        }




        [HttpPost("{graphId}/node-subgraph", Name = "NodeSubgraph")]
        public async Task<IActionResult> GetNodeSubgraphAsync([FromRoute] Guid graphId, [FromBody] SubGraphRequestDto subGraphRequest)
        {
            if (subGraphRequest == null)
                return BadRequest("Request body cannot be empty.");

            var data = await _pageGrapher.GetNodeSubgraphAsync(graphId, subGraphRequest.NodeUrl, subGraphRequest.MaxDepth, subGraphRequest.MaxNodes);

            if (data == null)
                return NotFound(new { message = $"No data found for graph {graphId}" });

            return Ok(data);
        }



        private GraphDto MapToDto(Graph graph) => new GraphDto
        {
            Id = graph.Id,
            Name = graph.Name,
            Description = graph.Description,
            CreatedAt = graph.CreatedAt,
            CrawlRequest = MapToDto(graph.CrawlRequest)
        };


        private CrawlRequestDto MapToDto(GraphCrawlRequest request) => new CrawlRequestDto
        {
            Url = request.Url?.AbsoluteUri,
            MaxDepth = request.MaxDepth,
            MaxLinks = request.MaxLinks,
            ExcludeExternalLinks = request.ExcludeExternalLinks,
            ExcludeQueryStrings = request.ExcludeQueryStrings,
            ConsolidateQueryStrings = request.ConsolidateQueryStrings,
            UrlMatchRegex = request.UrlMatchRegex,
            TitleElementXPath = request.TitleElementXPath,
            ContentElementXPath = request.ContentElementXPath,
            SummaryElementXPath = request.SummaryElementXPath,
            ImageElementXPath = request.ImageElementXPath,
            RelatedLinksElementXPath = request.RelatedLinksElementXPath
        };


        private GraphCrawlRequest MapToModel(CrawlRequestDto dto) => new GraphCrawlRequest
        {
            Url = Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) ? uri : null,
            MaxDepth = Math.Max(1, dto.MaxDepth),
            MaxLinks = Math.Max(1, dto.MaxLinks),
            ExcludeExternalLinks = dto.ExcludeExternalLinks,
            ExcludeQueryStrings = dto.ExcludeQueryStrings,
            ConsolidateQueryStrings = dto.ConsolidateQueryStrings,
            UrlMatchRegex = dto.UrlMatchRegex,
            TitleElementXPath = dto.TitleElementXPath,
            ContentElementXPath = dto.ContentElementXPath,
            SummaryElementXPath = dto.SummaryElementXPath,
            ImageElementXPath = dto.ImageElementXPath,
            RelatedLinksElementXPath = dto.RelatedLinksElementXPath
        };


    }

}
