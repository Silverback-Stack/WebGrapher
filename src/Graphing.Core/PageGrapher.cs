using Events.Core.Bus;
using Events.Core.Dtos;
using Events.Core.Events;
using Events.Core.Events.LogEvents;
using Events.Core.Helpers;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;
using System;

namespace Graphing.Core
{
    public class PageGrapher : IPageGrapher, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IWebGraph _webGraph;
        private readonly GraphingSettings _graphingSettings;
        private readonly IWebGraphPayloadSerializer _graphPayloadSerializer;

        public PageGrapher(
            ILogger logger, 
            IEventBus eventBus, 
            IWebGraph webGraph, 
            GraphingSettings graphingSettings, 
            IWebGraphPayloadSerializer graphPayloadSerializer)
        {
            _logger = logger;
            _eventBus = eventBus;
            _webGraph = webGraph;
            _graphingSettings = graphingSettings;
            _graphPayloadSerializer = graphPayloadSerializer;
        }


        public async Task StartAsync()
        {
            await _eventBus.SubscribeAsync<GraphPageEvent>(
                _graphingSettings.ServiceName, ProcessGraphPageEventAsync);

            await _eventBus.SubscribeAsync<GraphWriteToLogEvent>(
                _graphingSettings.ServiceName, ProcessGraphWriteToLogEventAsync);
        }


        public async Task StopAsync()
        {
            await _eventBus.UnsubscribeAsync<GraphPageEvent>(
                _graphingSettings.ServiceName, ProcessGraphPageEventAsync);

            await _eventBus.UnsubscribeAsync<GraphWriteToLogEvent>(
                _graphingSettings.ServiceName, ProcessGraphWriteToLogEventAsync);
        }


        /// <summary>
        /// Processes a Graph Page Event and maps the normalised page and its relationships to the WebGraph.
        /// </summary>
        private async Task ProcessGraphPageEventAsync(GraphPageEvent evt)
        {
            try
            {
                var request = evt.CrawlPageRequest;
                var result = evt.NormalisePageResult;

                // If graph creation is requested, create the WebGraph if it does not already exist 
                if (request.GraphCreationOptions != null)
                {
                    await _webGraph.EnsureGraphExistsAsync(
                        request.GraphId,
                        request.GraphCreationOptions.UserId,
                        new GraphOptions
                        {
                            Name = request.GraphCreationOptions.Name,
                            Description = request.GraphCreationOptions.Description,
                            Url = request.Url,
                            MaxLinks = request.Options.MaxLinks,
                            MaxDepth = request.Options.MaxDepth,
                            ExcludeExternalLinks = request.Options.ExcludeExternalLinks,
                            ExcludeQueryStrings = request.Options.ExcludeQueryStrings,
                            ConsolidateQueryStrings = request.Options.ConsolidateQueryStrings,
                            UrlMatchRegex = request.Options.UrlMatchRegex,
                            TitleElementXPath = request.Options.TitleElementXPath,
                            ContentElementXPath = request.Options.ContentElementXPath,
                            SummaryElementXPath = request.Options.SummaryElementXPath,
                            ImageElementXPath = request.Options.ImageElementXPath,
                            RelatedLinksElementXPath = request.Options.RelatedLinksElementXPath,
                            UserAgent = request.Options.UserAgent,
                            UserAccepts = request.Options.UserAccepts
                        });
                }


                // Map the normalised page result to PageData
                var pageData = new PageData
                {
                    Url = ResolvePageUrl(
                        request.Options.ConsolidateQueryStrings,
                        result.CanonicalUrl,
                        result.Url),
                    OriginalUrl = result.OriginalUrl.AbsoluteUri,
                    IsRedirect = result.IsRedirect,
                    SourceLastModified = result.SourceLastModified,
                    Title = result.Title,
                    Summary = result.Summary,
                    ImageUrl = result.ImageUrl?.AbsoluteUri,
                    ImageCors = result.ImageCors,
                    Keywords = result.Keywords,
                    Tags = result.Tags,
                    Links = result.Links?
                        .Select(l => l.AbsoluteUri)
                        ?? Enumerable.Empty<string>(),
                    DetectedLanguageIso3 = result.DetectedLanguageIso3,
                    ContentFingerprint = result.Fingerprint
                };


                // Callback delegates used by the WebGraph:

                // Called when a Node is populated with data
                Func<Node, Task> nodePopulatedCallback = node =>
                    PublishStreamNodePayloadEventAsync(request, node);

                // Called when Node population is requested
                Func<Node, Task> nodePopulationRequestCallback = node =>
                    PublishCrawlPageEventAsync(request, node);


                // Map the page and its relationships to the WebGraph
                await _webGraph.MapPageAsync(
                    request.GraphId,
                    pageData,
                    request.Depth,
                    nodePopulatedCallback,
                    nodePopulationRequestCallback);
            }
            catch (Exception ex)
            {
                // Crawling can encounter errors; failed pages may be crawled again later,
                // so a failure does not need to interrupt further graph processing.
                _logger.LogError(ex, "Error processing GraphPageEvent.");
            }
        }


        /// <summary>
        /// Resolves the URL used to identify the page, using the canonical URL when
        /// query strings are consolidated so different query strings map to the same Node.
        /// </summary>
        private string ResolvePageUrl(bool consolidateQueryStrings, Uri canonicalUrl, Uri url)
        {
            return consolidateQueryStrings
                    ? canonicalUrl.AbsoluteUri
                    : url.AbsoluteUri;
        }


        /// <summary>
        /// Publishes a streaming payload event for a populated Node and its relationships.
        /// </summary>
        private async Task PublishStreamNodePayloadEventAsync(CrawlPageRequestDto request, Node node)
        {
            //TODO: INITALLY JUST LOG THEN ADD IMPLEMENTATION IN LATER CHAPTER
            _logger.LogInformation(
                "Streaming node payload for {Url}: Not yet implemented.",
                node.Url);



            // ########################
            // Add below in API Chapter
            // ########################

            var payload = _graphPayloadSerializer.Serialize(node);
            payload.CorrelationId = request.CorrelationId;

            if (!payload.Nodes.Any() && !payload.Edges.Any())
                return;

            await _eventBus.PublishAsync(new StreamNodePayloadEvent
            {
                SigmaGraphPayload = payload
            });

            _logger.LogInformation("Streaming node payload: {Url} Nodes: {NodeCount} Edges: {EdgeCount}", 
                node.Url, payload.NodeCount, payload.EdgeCount);

            await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Information,
                    $"Streaming node payload: {node.Url} Nodes: {payload.NodeCount} Edges: {payload.EdgeCount}",
                    "GraphingNodePopulated",
                    new LogContext
                    {
                        Url = node.Url,
                        NodeCount = payload.NodeCount,
                        EdgeCount = payload.EdgeCount
                    });
        }


        /// <summary>
        /// Publishes a Crawl Page Event for a Node discovered through a page relationship.
        /// </summary>
        private async Task PublishCrawlPageEventAsync(CrawlPageRequestDto request, Node node)
        {
            // Discovered Nodes are crawled one level deeper than the current page
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

            // Schedule the crawl request by adding a small random delay.
            // This helps pace requests and reduce contention caused by bursts.
            var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(
                DateTimeOffset.UtcNow,
                _graphingSettings.ScheduleCrawlDelayMinSeconds,
                _graphingSettings.ScheduleCrawlDelayMaxSeconds);

            await _eventBus.PublishAsync(
                crawlPageEvent,
                priority: depth,
                scheduledOffset);

            _logger.LogInformation("Graphing Edge Discovered: {Url} Depth: {Depth}",
                node.Url, depth);

            await PublishClientLogEventAsync(
                    request.GraphId,
                    request.CorrelationId,
                    LogType.Information,
                    $"Graphing Edge Discovered: {node.Url} Depth: {depth}",
                    "GraphingEdgeDiscovered",
                    new LogContext
                    {
                        Url = node.Url,
                        Attempt = crawlPageRequest.Attempt,
                        Depth = crawlPageRequest.Depth
                    });
        }


        /// <summary>
        /// Processes a Graph Write To Log Event and writes a diagnostic view of the current WebGraph to the log.
        /// Used for inspection during development.
        /// </summary>
        private async Task ProcessGraphWriteToLogEventAsync(GraphWriteToLogEvent evt)
        {
            var graphDiagnosticView = await _webGraph.GetGraphDiagnosticViewAsync(evt.GraphId, evt.MaxDepth, evt.MaxNodes);
            _logger.LogInformation(graphDiagnosticView);
        }






        // ########################
        // Add below in API Chapter
        // ########################

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


        public async Task<Graph?> GetGraphByIdAsync(Guid graphId, string userId)
        {
            return await _webGraph.GetGraphAsync(graphId, userId);
        }

        public async Task<Graph> CreateGraphAsync(string userId, GraphOptions options)
        {
            return await _webGraph.CreateGraphAsync(Guid.NewGuid(), userId, options);
        }

        public async Task<Graph> UpdateGraphAsync(Graph graph, string userId)
        {
            return await _webGraph.UpdateGraphAsync(graph, userId);
        }

        public async Task<Graph?> DeleteGraphAsync(Guid graphId, string userId)
        {
            return await _webGraph.DeleteGraphAsync(graphId, userId);
        }

        public async Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize, string userId)
        {
            return await _webGraph.ListGraphsAsync(page, pageSize, userId);
        }



        public async Task<CrawlPageRequestDto> CrawlPageAsync(Guid graphId, GraphOptions options, bool preview)
        {
            //create a crawl page request
            var crawlPageRequest = new CrawlPageRequestDto
            {
                Url = options.Url,
                GraphId = graphId,
                CorrelationId = Guid.NewGuid(),
                Attempt = 1,
                Depth = 0,
                Preview = preview,
                Options = new CrawlPageRequestOptionsDto
                {
                    MaxDepth = options.MaxDepth,
                    MaxLinks = options.MaxLinks,
                    ExcludeExternalLinks = options.ExcludeExternalLinks,
                    ExcludeQueryStrings = options.ExcludeQueryStrings,
                    ConsolidateQueryStrings = options.ConsolidateQueryStrings,
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


        public async Task<SigmaGraphPayloadDto> PopulateClientGraphAsync(Guid graphId, int maxDepth, int? maxNodes = null)
        {
            //Clamp values
            maxDepth = Math.Clamp(maxDepth, 0, _graphingSettings.MaxRequestDepthLimit);
            if (maxNodes.HasValue)
                maxNodes = Math.Clamp(maxNodes.Value, 1, _graphingSettings.MaxRequestNodeLimit);

            var initalNodes = await _webGraph.GetInitialGraphNodes(graphId, 1);
            var startNode = initalNodes.FirstOrDefault();

            // 2. Traverse the graph if a start node exists
            var nodes = startNode != null
                ? await _webGraph.GetNodeNeighborhoodAsync(graphId, startNode.Url, maxDepth, maxNodes)
                : Enumerable.Empty<Node>();

            // 3. If no nodes found, return empty payload
            if (!nodes.Any())
                return _graphPayloadSerializer.Empty(graphId);

            // 4. Build and return payload
            return _graphPayloadSerializer.Serialize(nodes, graphId);
        }

        public async Task<SigmaGraphPayloadDto> GetNodeSubgraphAsync(Guid graphId, Uri nodeUrl, int maxDepth = 1, int? maxNodes = null)
        {
            maxDepth = Math.Clamp(maxDepth, 1, _graphingSettings.MaxRequestDepthLimit);
            if (maxNodes.HasValue)
                maxNodes = Math.Clamp(maxNodes.Value, 1, _graphingSettings.MaxRequestNodeLimit);

            var nodes = nodeUrl != null
                ? await _webGraph.GetNodeNeighborhoodAsync(graphId, nodeUrl.AbsoluteUri, maxDepth, maxNodes)
                : Enumerable.Empty<Node>();

            if (!nodes.Any())
                return _graphPayloadSerializer.Empty(graphId);


            return _graphPayloadSerializer.Serialize(nodes, graphId);
        }


    }
}
