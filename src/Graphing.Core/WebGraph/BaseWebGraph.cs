using System;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.WebGraph
{
    public abstract class BaseWebGraph : IWebGraph
    {
        protected readonly ILogger _logger;
        protected readonly GraphingSettings _graphingSettings;
        
        protected BaseWebGraph(
            ILogger logger, 
            GraphingSettings graphingSettings)
        {
            _logger = logger;
            _graphingSettings = graphingSettings;
        }

        public async Task EnsureGraphExistsAsync(GraphOptions options)
        {
            var graph = await GetGraphAsync(options.GraphId, options.UserId);

            if (graph != null)
            {
                return;
            }

            await CreateGraphAsync(options);
        }


        /// <summary>
        /// Maps page data to a graph node and its outgoing relationships.
        /// </summary>
        /// <param name="graphId">The ID of the graph to map the page to.</param>
        /// <param name="pageData">The page data to map.</param>
        /// <param name="crawlDepth">The current crawl depth.</param>
        /// <param name="nodePopulatedCallback">Callback invoked when the node has been populated.</param>
        /// <param name="nodePopulationRequestCallback">Callback invoked when population of a node is requested.</param>
        public async Task MapPageAsync(
            Guid graphId,
            PageData pageData, 
            int crawlDepth,
            Func<Node, Task>? nodePopulatedCallback, 
            Func<Node, Task>? nodePopulationRequestCallback)
        {
            _logger.LogDebug("Mapping page for {Url}", pageData.Url);

            // Mark or promote URL as Populated
            var node = await GetOrCreateNodeAsync(graphId, pageData.Url, NodeState.Populated);

            if (!ShouldUpdateNode(pageData, node, crawlDepth))
            {
                _logger.LogDebug("No updated required for {Url} - content has not changed.", pageData.Url);
                return;
            }

            await PopulateNodeAsync(node, pageData);

            if (_graphingSettings.WebGraph.OutgoingNodesUpdateMode == OutgoingNodesUpdateMode.Replace)
            {
                await ClearOutgoingNodesAsync(graphId, node);
            }

            if (pageData.IsRedirect)
            {
                _logger.LogDebug("Handling redirect {OriginalUrl} -> {Url}",
                    pageData.OriginalUrl, pageData.Url);
                await MarkNodeAsRedirectedAsync(graphId, pageData.OriginalUrl, pageData.Url);
            }

            var nodesToPopulate = await AddOutgoingNodesAsync(graphId, pageData, crawlDepth);

            node = await ReloadNodeAsync(node);
            if (node is null) return;

            if (nodePopulatedCallback != null)
            {
                await nodePopulatedCallback(node);
            }

            if (nodePopulationRequestCallback != null)
            {
                await RequestNodePopulationAsync(nodesToPopulate, nodePopulationRequestCallback, crawlDepth);
            }

            // Uncomment to outoput graph data dump for testing only (will incure performance hit)
            //var dataDump = await DumpGraphContentsAsync(webPage.GraphId);
            //_logger.LogInformation(dataDump);
        }

        private async Task<Node?> ReloadNodeAsync(Node node)
        {
            return await GetNodeAsync(node.GraphId, node.Url);
        }

        private async Task<IEnumerable<Node>> AddOutgoingNodesAsync(Guid graphId, PageData pageData, int crawlDepth)
        {
            var addedNodes = new HashSet<Node>();

            // Add new outgoing nodes (if any)
            foreach (var link in pageData.Links)
            {
                var targetNode = await AddNodeRelationshipAsync(
                    graphId,
                    pageData.Url, 
                    link,
                    crawlDepth);

                if (targetNode != null)
                {
                    addedNodes.Add(targetNode);
                }
            }

            return addedNodes;
        }

        private async Task RequestNodePopulationAsync(
            IEnumerable<Node> nodes, 
            Func<Node, Task> onNodePopulationRequest, 
            int crawlDepth)
        {
            foreach (var node in nodes)
            {
                // Initial crawls bypass the Node refresh throttle
                if (CanRefreshNode(node, crawlDepth))
                {
                    node.AllowRefreshAfter = DateTimeOffset.UtcNow.AddSeconds(
                        _graphingSettings.WebGraph.NodeRefreshThrottleSeconds);

                    await SaveNodeAsync(node);

                    await onNodePopulationRequest(node);
                }
            }
        }

        protected async Task<Node> GetOrCreateNodeAsync(
            Guid graphId,
            string url,
            NodeState state = NodeState.Dummy)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            var node = await GetNodeAsync(graphId, url);
            if (node == null)
            {
                _logger.LogDebug(
                    "Creating new node for {Url} with state {State}",
                    url,
                    state);

                node = new Node(graphId, url, state);
                await SaveNodeAsync(node);
            }
            else if (state == NodeState.Populated &&
                node.State == NodeState.Dummy)
            {
                await MarkNodeAsPopulatedAsync(node);
            }

            return node!;
        }

        protected async Task PopulateNodeAsync(Node node, PageData pageData)
        {
            node.Title = pageData.Title ?? string.Empty;
            node.Summary = pageData.Summary ?? string.Empty;
            node.ImageUrl = pageData.ImageUrl ?? string.Empty;
            node.ImageCors = pageData.ImageCors;
            node.Keywords = pageData.Keywords ?? string.Empty;
            node.Tags = pageData.Tags ?? Enumerable.Empty<string>();
            node.SourceLastModified = pageData.SourceLastModified;
            node.ContentFingerprint = pageData.ContentFingerprint;
            await SaveNodeAsync(node);
        }

        /// <summary>
        /// Returns target Node if a relationship was added.
        /// </summary>
        protected async Task<Node?> AddNodeRelationshipAsync(Guid graphId, string sourceUrl, string targetUrl, int crawlDepth)
        {
            if (sourceUrl == targetUrl)
                return null; // Ignore self-referencing relationships

            if (string.IsNullOrEmpty(sourceUrl)) throw new ArgumentNullException(nameof(sourceUrl));
            if (string.IsNullOrEmpty(targetUrl)) throw new ArgumentNullException(nameof(targetUrl));

            var sourceNode = await GetOrCreateNodeAsync(graphId, sourceUrl, NodeState.Populated);
            var targetNode = await GetOrCreateNodeAsync(graphId, targetUrl, NodeState.Dummy);

            var relationshipAdded = await AddOutgoingNodeAsync(graphId, sourceNode, targetNode);

            // Include new relationships, or existing relationships during an initial crawl.
            if (relationshipAdded || crawlDepth == 0)
            {
                _logger.LogDebug("Adding outgoing/incoming nodes from {Url} to {Url}",
                    sourceNode.Url, targetNode.Url);

                await AddIncomingNodeAsync(graphId, targetNode, sourceNode);

                // Update popularity scores
                sourceNode.PopularityScore = await GetPopularityScoreAsync(graphId, sourceNode);
                targetNode.PopularityScore = await GetPopularityScoreAsync(graphId, targetNode);

                await SaveNodeAsync(sourceNode);
                await SaveNodeAsync(targetNode);

                return targetNode;
            }

            // Relationship already exists
            return null;
        }

        /// <summary>
        /// Returns True if a Node is permitted to be refreshed.
        /// </summary>
        protected bool CanRefreshNode(Node node, int crawlDepth)
        {
            // Initial crawls bypass the Node refresh throttle
            if (crawlDepth == 0) return true;

            // Otherwise, refresh only when the Node has no throttle or the throttle has expired
            if (node.AllowRefreshAfter == null ||
                DateTimeOffset.UtcNow >= node.AllowRefreshAfter.Value)
            {
                return true;
            }

            _logger.LogDebug(
                "Node refresh for {Url} throttled. Next eligible time: {AllowRefreshAfter}",
                node.Url,
                node.AllowRefreshAfter);

            return false;
        }


        protected async Task MarkNodeAsPopulatedAsync(Node node)
        {
            _logger.LogDebug("Promoting dummy node {Url} to populated.", node.Url);

            node.State = NodeState.Populated;
            await SaveNodeAsync(node);
        }

        protected async Task MarkNodeAsRedirectedAsync(Guid graphId, string sourceUrl, string targetUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl)) throw new ArgumentNullException(nameof(sourceUrl));
            if (string.IsNullOrEmpty(targetUrl)) throw new ArgumentNullException(nameof(targetUrl));

            var sourceNode = await GetOrCreateNodeAsync(graphId, sourceUrl);
            var targetNode = await GetOrCreateNodeAsync(graphId, targetUrl);

            if (sourceNode.State == NodeState.Populated)
            {
                _logger.LogDebug("Skipping redirect for {SourceUrl} – already populated.", sourceUrl);
                return;
            }

            // Mark as redirected
            sourceNode.State = NodeState.Redirected;
            sourceNode.RedirectedToUrl = targetUrl;

            // Maintain incoming/outgoing nodes
            await AddOutgoingNodeAsync(graphId, sourceNode, targetNode);
            await AddIncomingNodeAsync(graphId, targetNode, sourceNode);

            _logger.LogDebug("Marked node {SourceUrl} as redirected to {TargetUrl} and updated nodes.",
                sourceUrl, targetUrl);

            // Update popularity
            sourceNode.PopularityScore = await GetPopularityScoreAsync(graphId, sourceNode);
            targetNode.PopularityScore = await GetPopularityScoreAsync(graphId, targetNode);

            await SaveNodeAsync(targetNode);
            await SaveNodeAsync(sourceNode);
        }

        /// <summary>
        /// Determines whether a node needs to be updated based on the current state of the associated page data.
        /// </summary>
        /// <param name="crawlDepth">
        /// The current crawl depth. A depth of 0 represents the initial crawl
        /// and bypasses the Node refresh throttle.
        /// </param>
        private bool ShouldUpdateNode(PageData pageData, Node node, int crawlDepth)
        {
            if (node == null) return true;

            return node.State != NodeState.Populated ||
               crawlDepth == 0 ||
               node.ContentFingerprint != pageData.ContentFingerprint ||
               node.SourceLastModified != pageData.SourceLastModified;
        }

        /// <summary>
        /// Persists any changes to Node and sets ModifiedAt to now.
        /// </summary>
        private async Task SaveNodeAsync(Node node)
        {
            node.ModifiedAt = DateTimeOffset.UtcNow;
            await SetNodeAsync(node);
        }



        // Node Abstractions
        public abstract Task<Node?> GetNodeAsync(Guid graphId, string url);

        public abstract Task<Node> SetNodeAsync(Node node);

        protected abstract Task<bool> AddOutgoingNodeAsync(Guid graphId, Node sourceNode, Node targetNode);
        
        protected abstract Task<bool> AddIncomingNodeAsync(Guid graphId, Node targetNode, Node sourceNode);
        
        protected abstract Task ClearOutgoingNodesAsync(Guid graphId, Node node);

        public abstract Task CleanupOrphanedNodesAsync(Guid graphId);

        protected abstract Task<int> GetPopularityScoreAsync(Guid graphId, Node node);

        public abstract Task<IEnumerable<Node>> GetInitialGraphNodes(Guid graphId, int topN);

        public abstract Task<long> TotalPopulatedNodesAsync(Guid graphID);

        public abstract Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);

        public abstract Task<string> DumpGraphContentsAsync(Guid graphId);



        //Graph Abstractions
        public abstract Task<Graph?> GetGraphAsync(Guid graphId, string? userId);

        public abstract Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize, string userId);

        public abstract Task<Graph> CreateGraphAsync(GraphOptions options);

        public abstract Task<Graph> UpdateGraphAsync(Graph graph, string userId);

        public abstract Task<Graph?> DeleteGraphAsync(Guid graphId, string userId);


    }
}
