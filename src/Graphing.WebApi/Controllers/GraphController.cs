using Graphing.Core;
using Graphing.Core.WebGraph.Dtos;
using Graphing.Core.WebGraph.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Graphing.WebApi.Controllers
{
 
    [ApiController]
    [Route("api/[controller]")]
    public class GraphController : ControllerBase
    {
        private readonly IPageGrapher _pageGrapher;

        public GraphController(IPageGrapher pageGrapher)
        {
            _pageGrapher = pageGrapher;
        }

        // GET existing graph
        [HttpGet("{graphId}", Name = "GetById")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] Guid graphId)
        {
            var graph = await _pageGrapher.GetGraphByIdAsync(graphId);
            if (graph == null) return NotFound();

            var graphDto = MapToDto(graph);
            return Ok(graphDto);
        }

        // POST create new graph
        [HttpPost("create", Name = "Create")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateGraphDto createGraph)
        {
            if (createGraph == null)
                return BadRequest("Request body cannot be empty.");

            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            var newGraph = await _pageGrapher.CreateGraphAsync(new GraphOptions
            {
                Name = createGraph.Name,
                Description = createGraph.Description,
                UserAgent = userAgent ?? GraphOptions.DEFAULT_USER_AGENT
            });

            if (newGraph == null)
            {
                return StatusCode(500, "Failed to create graph.");
            }

            var createdDto = MapToDto(newGraph);

            // Return 201 Created with route to the new graph
            return CreatedAtRoute(
                "GetById",
                new { graphId = newGraph.Id },
                createdDto
            );
        }

        [HttpPut("{graphId}/update", Name = "Update")]
        public async Task<IActionResult> UpdateGraphAsync([FromRoute] Guid graphId, [FromBody] UpdateGraphDto updateGraph)
        {
            if (updateGraph == null)
                return BadRequest("Request body cannot be empty.");

            var existingGraph = await _pageGrapher.GetGraphByIdAsync(graphId);
            if (existingGraph == null)
                return NotFound();

            if (!Uri.TryCreate(updateGraph.Url, UriKind.Absolute, out var validatedUrl))
            {
                return BadRequest("Invalid URL format.");
            }

            existingGraph.Name = updateGraph.Name;
            existingGraph.Description = updateGraph.Description;
            existingGraph.Url = validatedUrl.AbsoluteUri;
            existingGraph.MaxDepth = Math.Max(1, updateGraph.MaxDepth);
            existingGraph.MaxLinks = Math.Max(1, updateGraph.MaxLinks);
            existingGraph.ExcludeExternalLinks = updateGraph.ExcludeExternalLinks;
            existingGraph.ExcludeQueryStrings = updateGraph.ExcludeQueryStrings;
            existingGraph.UrlMatchRegex = updateGraph.UrlMatchRegex;
            existingGraph.TitleElementXPath = updateGraph.TitleElementXPath;
            existingGraph.ContentElementXPath = updateGraph.ContentElementXPath;
            existingGraph.SummaryElementXPath = updateGraph.SummaryElementXPath;
            existingGraph.ImageElementXPath = updateGraph.ImageElementXPath;
            existingGraph.RelatedLinksElementXPath = updateGraph.RelatedLinksElementXPath;

            var updatedGraph = await _pageGrapher.UpdateGraphAsync(existingGraph);

            if (updatedGraph == null)
            {
                return StatusCode(500, "Failed to update graph.");
            }

            return Ok(MapToDto(updatedGraph));
        }

        

        [HttpPost("{graphId}/crawl", Name = "Crawl")]
        public async Task<IActionResult> CrawlPageAsync([FromRoute] Guid graphId, [FromBody] CrawlPageDto crawlPage)
        {
            if (crawlPage == null)
                return BadRequest("Request body cannot be empty.");

            if (!Uri.TryCreate(crawlPage.Url, UriKind.Absolute, out var validatedUrl))
            {
                ModelState.AddModelError(nameof(crawlPage.Url), "Invalid URL. Use format https://www.example.com");
                return ValidationProblem();
            }

            if (crawlPage.OverwriteDefaults)
            {
                //overwrite graph defaults with new values from crawl page request
                var existingGraph = await _pageGrapher.GetGraphByIdAsync(graphId);
                if (existingGraph == null)
                    return NotFound();

                existingGraph.Url = validatedUrl.AbsoluteUri;
                existingGraph.MaxDepth = Math.Max(1, crawlPage.MaxDepth);
                existingGraph.MaxLinks = Math.Max(1, crawlPage.MaxLinks);
                existingGraph.ExcludeExternalLinks = crawlPage.ExcludeExternalLinks;
                existingGraph.ExcludeQueryStrings = crawlPage.ExcludeQueryStrings;
                existingGraph.UrlMatchRegex = crawlPage.UrlMatchRegex;
                existingGraph.TitleElementXPath = crawlPage.TitleElementXPath;
                existingGraph.ContentElementXPath = crawlPage.ContentElementXPath;
                existingGraph.SummaryElementXPath = crawlPage.SummaryElementXPath;
                existingGraph.ImageElementXPath = crawlPage.ImageElementXPath;
                existingGraph.RelatedLinksElementXPath = crawlPage.RelatedLinksElementXPath;

                var updatedGraph = await _pageGrapher.UpdateGraphAsync(existingGraph);

                if (updatedGraph == null)
                {
                    return StatusCode(500, "Failed to update graph.");
                }
            }

            //submit a crawl page request:
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            var crawlPageRequestDto = await _pageGrapher.CrawlPageAsync(graphId, new GraphOptions
            {
                Url = validatedUrl,
                MaxDepth = Math.Max(1, crawlPage.MaxDepth),
                MaxLinks = Math.Max(1, crawlPage.MaxLinks),
                ExcludeExternalLinks = crawlPage.ExcludeExternalLinks,
                ExcludeQueryStrings = crawlPage.ExcludeQueryStrings,
                UrlMatchRegex = crawlPage.UrlMatchRegex,
                TitleElementXPath = crawlPage.TitleElementXPath,
                ContentElementXPath = crawlPage.ContentElementXPath,
                SummaryElementXPath = crawlPage.SummaryElementXPath,
                ImageElementXPath = crawlPage.ImageElementXPath,
                RelatedLinksElementXPath = crawlPage.RelatedLinksElementXPath,
                UserAgent = string.IsNullOrEmpty(userAgent) ? GraphOptions.DEFAULT_USER_AGENT : userAgent,
                UserAccepts = GraphOptions.DEFAULT_USER_ACCEPTS, //always use default as crawler currently only supports text & html
                Preview = crawlPage.Preview
            });

            return Ok(crawlPageRequestDto);
        }


        [HttpDelete("{graphId}/delete", Name = "Delete")]
        public async Task<IActionResult> DeleteGraphAsync([FromRoute] Guid graphId)
        {
            var deletedGraph = await _pageGrapher.DeleteGraphAsync(graphId);

            if (deletedGraph == null)
                return NotFound();

            // Successfully deleted
            return NoContent();
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListGraphsAsync(int page = 1, int pageSize = 8)
        {
            if (pageSize > 20) pageSize = 20;

            var result = await _pageGrapher.ListGraphsAsync(page, pageSize);

            var dtoResult = new PagedResult<ListGraphDto>(
                result.Items.Select(g => new ListGraphDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CreatedAt = g.CreatedAt,
                    Url = g.Url.ToString()
                }),
                result.TotalCount,
                result.Page,
                result.PageSize
            );

            return Ok(dtoResult);
        }

        [HttpGet("{graphId}/populate", Name = "Populate")]
        public async Task<IActionResult> PopulateGraphAsync([FromRoute] Guid graphId, [FromQuery] int maxDepth, [FromQuery] int? maxNodes = null)
        {
            var data = await _pageGrapher.PopulateGraphAsync(graphId, maxDepth, maxNodes);

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
            Url = graph.Url,
            MaxDepth = graph.MaxDepth,
            MaxLinks = graph.MaxLinks,
            ExcludeExternalLinks = graph.ExcludeExternalLinks,
            ExcludeQueryStrings = graph.ExcludeQueryStrings,
            UrlMatchRegex = graph.UrlMatchRegex,
            TitleElementXPath = graph.TitleElementXPath,
            ContentElementXPath = graph.ContentElementXPath,
            SummaryElementXPath = graph.SummaryElementXPath,
            ImageElementXPath = graph.ImageElementXPath,
            RelatedLinksElementXPath = graph.RelatedLinksElementXPath
        };
    }
}
