using System;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Helpers;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Adapters.SigmaJs;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core
{
    public class PageGrapher : IPageGrapher, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IWebGraph _webGraph;

        public PageGrapher(ILogger logger, IEventBus eventBus, IWebGraph webGraph)
        {
            _logger = logger;
            _eventBus = eventBus;
            _webGraph = webGraph;
        }
        public void SubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        private async Task ProcessGraphPageEventAsync(GraphPageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var webPage = MapToWebPage(evt);

            //Delegate : Called when Node is populated with data
            Func<Node, Task> onNodePopulated = async (node) =>
            {
                var payload = SigmaJsGraphPayloadBuilder.BuildPayload(node);

                if (!payload.Nodes.Any() && !payload.Edges.Any())
                    return;

                _logger.LogDebug($"Publishing node populated event for {node.Url} " +
                     $"(nodes: {payload.NodeCount}, edges: {payload.EdgeCount})");

                await _eventBus.PublishAsync(new GraphNodeAddedEvent
                {
                    SigmaGraphPayload = payload
                });
            };

            //Delegate : Called when Link is discovered
            Func<Node, Task> onLinkDiscovered = async (node) =>
            {
                var depth = request.Depth + 1;

                var crawlPageRequest = request with
                {
                    Url = new Uri(node.Url),
                    Attempt = 1,
                    Depth = depth
                };

                var crawlPageEvent = new CrawlPageEvent
                {
                    CrawlPageRequest = crawlPageRequest,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);

                _logger.LogDebug($"Scheduling crawl for link {node.Url} at depth {depth}");

                await _eventBus.PublishAsync(
                    crawlPageEvent, 
                    priority: depth, 
                    scheduledOffset);
            };

            await _webGraph.AddWebPageAsync(webPage, onNodePopulated, onLinkDiscovered);
        }

        private WebPageItem MapToWebPage(GraphPageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var result = evt.NormalisePageResult;

            return new WebPageItem
            {
                GraphId = request.GraphId,
                Url = result.Url.AbsoluteUri,
                OriginalUrl = result.OriginalUrl.AbsoluteUri,
                IsRedirect = result.IsRedirect,
                SourceLastModified = result.SourceLastModified,
                Title = result.Title,
                Summary = result.Summary,
                ImageUrl = result.ImageUrl?.AbsoluteUri,
                Keywords = result.Keywords,
                Tags = result.Tags,
                Links = result.Links?.Select(l => l.AbsoluteUri) ?? Enumerable.Empty<string>(),
                DetectedLanguageIso3 = result.DetectedLanguageIso3,
            };
        }

        public async Task<Graph?> GetGraphByIdAsync(Guid graphId)
        {
            return await _webGraph.GetGraphByIdAsync(graphId);
        }

        public async Task<Graph?> CreateGraphAsync(GraphOptions options)
        {
            var newGraph = await _webGraph.CreateGraphAsync(options);

            if (newGraph != null) {

                //create a crawl page request
                var crawlPageRequest = new CrawlPageRequestDto
                {
                    Url = options.Url,
                    GraphId = newGraph.Id,
                    CorrelationId = Guid.NewGuid(),
                    Attempt = 1,
                    Depth = 0,
                    Options = new CrawlPageRequestOptionsDto
                    {
                        MaxDepth = options.MaxDepth,
                        MaxLinks = options.MaxLinks,
                        ExcludeExternalLinks = options.ExcludeExternalLinks,
                        ExcludeQueryStrings = options.ExcludeQueryStrings,
                        UrlMatchRegex = options.UrlMatchRegex,
                        TitleElementXPath = options.TitleElementXPath,
                        ContentElementXPath = options.ContentElementXPath,
                        SummaryElementXPath = options.SummaryElementXPath,
                        ImageElementXPath = options.ImageElementXPath,
                        RelatedLinksElementXPath = options.RelatedLinksElementXPath,
                        UserAgent = options.UserAgent,
                        UserAccepts = options.UserAccepts
                    },
                    RequestedAt = DateTimeOffset.UtcNow
                };

                await PublishCrawlPageEvent(crawlPageRequest);
            }

            return newGraph;
        }

        public async Task<Graph> UpdateGraphAsync(Graph graph)
        {
            return await _webGraph.UpdateGraphAsync(graph);
        }

        public async Task<Graph?> DeleteGraphAsync(Guid graphId)
        {
            return await _webGraph.DeleteGraphAsync(graphId);
        }

        public async Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize)
        {
            return await _webGraph.ListGraphsAsync(page, pageSize);
        }

        public async Task CrawlPageAsync(Guid graphId, GraphOptions options)
        {
            //create a crawl page request
            var crawlPageRequest = new CrawlPageRequestDto
            {
                Url = options.Url,
                GraphId = graphId,
                CorrelationId = Guid.NewGuid(),
                Attempt = 1,
                Depth = 0,
                Options = new CrawlPageRequestOptionsDto
                {
                    MaxDepth = options.MaxDepth,
                    MaxLinks = options.MaxLinks,
                    ExcludeExternalLinks = options.ExcludeExternalLinks,
                    ExcludeQueryStrings = options.ExcludeQueryStrings,
                    UrlMatchRegex = options.UrlMatchRegex,
                    TitleElementXPath = options.TitleElementXPath,
                    ContentElementXPath = options.ContentElementXPath,
                    SummaryElementXPath = options.SummaryElementXPath,
                    ImageElementXPath = options.ImageElementXPath,
                    RelatedLinksElementXPath = options.RelatedLinksElementXPath,
                    UserAgent = options.UserAgent,
                    UserAccepts = options.UserAccepts
                },
                RequestedAt = DateTimeOffset.UtcNow
            };

            await PublishCrawlPageEvent(crawlPageRequest);
        }

        public async Task<SigmaGraphPayloadDto> TraverseGraphAsync(Guid graphId, Uri startUrl, int maxDepth, int? maxNodes = null)
        {
            var nodes = await _webGraph.TraverseGraphAsync(graphId, startUrl.AbsoluteUri, maxDepth, maxNodes);
            return SigmaJsGraphPayloadBuilder.BuildPayload(nodes, graphId);
        }

        public async Task<SigmaGraphPayloadDto> PopulateGraphAsync(Guid graphId, int maxDepth, int? maxNodes = null)
        {
            var popularNodes = await _webGraph.GetMostPopularNodes(graphId, 1);
            var startNode = popularNodes.FirstOrDefault();

            // 2. Traverse the graph if a start node exists
            var nodes = startNode != null
                ? await _webGraph.TraverseGraphAsync(graphId, startNode.Url, maxDepth, maxNodes)
                : Enumerable.Empty<Node>();

            // 3. If no nodes found, return empty payload
            if (!nodes.Any())
            {
                return new SigmaGraphPayloadDto
                {
                    GraphId = graphId,
                    Nodes = Array.Empty<SigmaGraphNodeDto>(),
                    Edges = Array.Empty<SigmaGraphEdgeDto>()
                };
            }

            // 4. Build and return payload
            return SigmaJsGraphPayloadBuilder.BuildPayload(nodes, graphId);
        }

        private async Task PublishCrawlPageEvent(CrawlPageRequestDto crawlPageRequest)
        {
            //create a crawl page event
            var crawlPageEvent = new CrawlPageEvent
            {
                CrawlPageRequest = crawlPageRequest,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Publish Crawl Event
            await _eventBus.PublishAsync(crawlPageEvent);
        }
    }
}
