using System;
using System.Text.Json;
using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
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
        private readonly GraphingSettings _graphingSettings;

        public PageGrapher(ILogger logger, IEventBus eventBus, IWebGraph webGraph, GraphingSettings graphingSettings)
        {
            _logger = logger;
            _eventBus = eventBus;
            _webGraph = webGraph;
            _graphingSettings = graphingSettings;
        }
        public void SubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Unsubscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        public async Task PublishClientLogEventAsync(
            Guid graphId,
            Guid? correlationId,
            LogType type,
            string message,
            string? code = null,
            Object? context = null)
        {
            var clientLogEvent = new ClientLogEvent
            {
                GraphId = graphId,
                CorrelationId = correlationId,
                Type = type,
                Message = message,
                Code = code,
                Service = _graphingSettings.ServiceName,
                Context = context
            };

            await _eventBus.PublishAsync(clientLogEvent);
        }

        private async Task PublishGraphNodeAddedEventAsync(CrawlPageRequestDto request, Node node)
        {
            var payload = SigmaJsGraphPayloadBuilder.BuildPayload(node, _graphingSettings);
            payload.CorrolationId = request.CorrelationId;

            if (!payload.Nodes.Any() && !payload.Edges.Any())
                return;

            await _eventBus.PublishAsync(new GraphNodeAddedEvent
            {
                SigmaGraphPayload = payload
            });

            var logMessage = $"Graphing Node Populated: {node.Url} Nodes: {payload.NodeCount} Edges: {payload.EdgeCount}";
            _logger.LogInformation(logMessage);

            await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Information,
                    logMessage,
                    "GraphingNodePopulated",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        NodeCount = payload.NodeCount,
                        EdgeCount = payload.EdgeCount
                    });

            var jsonPayload = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            // Log the serialized payload
            _logger.LogInformation("Sigma Payload:\n{Payload}", jsonPayload);
        }

        private async Task PublishCrawlPageEventAsync(CrawlPageRequestDto request, Node node)
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

            var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(
                DateTimeOffset.UtcNow,
                _graphingSettings.ScheduleCrawlDelayMinSeconds,
                _graphingSettings.ScheduleCrawlDelayMaxSeconds);

            await _eventBus.PublishAsync(
                crawlPageEvent,
                priority: depth,
                scheduledOffset);

            var logMessage = $"Graphing Edge Discovered: {node.Url} Depth {depth} Attempt: {crawlPageRequest.Attempt}";
            _logger.LogInformation(logMessage);

            await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Information,
                    logMessage,
                    "GraphingEdgeDiscovered",
                    new LogContext
                    {
                        Url = request.Url.AbsoluteUri,
                        Attempt = crawlPageRequest.Attempt,
                        EdgeCount = crawlPageRequest.Depth
                    });
        }


        private async Task ProcessGraphPageEventAsync(GraphPageEvent evt)
        {
            var request = evt.CrawlPageRequest;
            var webPage = MapToWebPage(evt);

            //Delegate : Called when Node is populated with data
            Func<Node, Task> nodePopulatedCallback = async (node) =>
            {
                await PublishGraphNodeAddedEventAsync(request, node);
            };

            //Delegate : Called when Link is discovered
            Func<Node, Task> linkDiscoveredCallback = async (node) =>
            {
                await PublishCrawlPageEventAsync(request, node);
            };

            // when Depth is 0 the request was initiated by the user
            // user initiated requests should force update of any previously stored information
            var forceRefresh = request.Depth == 0;

            await _webGraph.AddWebPageAsync(webPage, forceRefresh, nodePopulatedCallback, linkDiscoveredCallback);
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
                ContentFingerprint = result.ContentFingerprint
            };
        }

        public async Task<Graph?> GetGraphByIdAsync(Guid graphId)
        {
            return await _webGraph.GetGraphByIdAsync(graphId);
        }

        public async Task<Graph?> CreateGraphAsync(GraphOptions options)
        {
            return await _webGraph.CreateGraphAsync(options);
        }

        public async Task<Graph?> UpdateGraphAsync(Graph graph)
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

        public async Task<CrawlPageRequestDto> CrawlPageAsync(Guid graphId, GraphOptions options)
        {
            //create a crawl page request
            var crawlPageRequest = new CrawlPageRequestDto
            {
                Url = options.Url,
                GraphId = graphId,
                CorrelationId = Guid.NewGuid(),
                Attempt = 1,
                Depth = 0,
                Preview = options.Preview,
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

            await PublishCrawlPageEventAsync(crawlPageRequest);

            return crawlPageRequest;
        }

        public async Task<SigmaGraphPayloadDto> PopulateGraphAsync(Guid graphId, int maxDepth, int? maxNodes = null)
        {
            maxDepth = Math.Clamp(maxDepth, 1, _graphingSettings.MaxRequestDepthLimit);
            if (maxNodes.HasValue)
                maxNodes = Math.Clamp(maxNodes.Value, 1, _graphingSettings.MaxRequestNodeLimit);

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
            return SigmaJsGraphPayloadBuilder.BuildPayload(nodes, graphId, _graphingSettings);
        }

        public async Task<SigmaGraphPayloadDto> GetNodeSubgraphAsync(Guid graphId, Uri nodeUrl, int maxDepth = 1, int? maxNodes = null)
        {
            maxDepth = Math.Clamp(maxDepth, 1, _graphingSettings.MaxRequestDepthLimit);
            if (maxNodes.HasValue)
                maxNodes = Math.Clamp(maxNodes.Value, 1, _graphingSettings.MaxRequestNodeLimit);

            var nodes = nodeUrl != null
                ? await _webGraph.TraverseGraphAsync(graphId, nodeUrl.AbsoluteUri, maxDepth, maxNodes)
                : Enumerable.Empty<Node>();

            if (!nodes.Any())
            {
                return new SigmaGraphPayloadDto
                {
                    GraphId = graphId,
                    Nodes = Array.Empty<SigmaGraphNodeDto>(),
                    Edges = Array.Empty<SigmaGraphEdgeDto>()
                };
            }

            return SigmaJsGraphPayloadBuilder.BuildPayload(nodes, graphId, _graphingSettings);
        }

        private async Task PublishCrawlPageEventAsync(CrawlPageRequestDto crawlPageRequest)
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
