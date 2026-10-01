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


        /// <summary>
        /// Creates a WebGraph with the supplied identity, owner and options.
        /// </summary>
        public abstract Task<Graph> CreateGraphAsync(Guid graphId, string userId, GraphOptions options);

        /// <summary>
        /// Creates a WebGraph with the supplied identity, owner and options.
        /// </summary>
        public abstract Task<Graph?> GetGraphAsync(Guid graphId, string userId);

        /// <summary>
        /// Retrieves a WebGraph by its ID and owner.
        /// </summary>
        public abstract Task<Node?> GetNodeAsync(Guid graphId, string url);

        /// <summary>
        /// Saves a Node to the WebGraph.
        /// </summary>
        public abstract Task<Node> SetNodeAsync(Node node);

        /// <summary>
        /// Adds an outgoing relationship from a source Node to a target Node.
        /// </summary>
        protected abstract Task<bool> AddOutgoingRelationshipAsync(Guid graphId, Node sourceNode, Node targetNode);

        /// <summary>
        /// Adds an incoming relationship to a target Node from a source Node.
        /// </summary>
        protected abstract Task<bool> AddIncomingRelationshipAsync(Guid graphId, Node targetNode, Node sourceNode);

        /// <summary>
        /// Removes all outgoing relationships from a Node.
        /// </summary>
        protected abstract Task ClearOutgoingRelationshipsAsync(Guid graphId, Node node);

        /// <summary>
        /// Calculates the popularity score for a Node.
        /// </summary>
        protected abstract Task<int> GetPopularityScoreAsync(Guid graphId, Node node);


        /// <summary>
        /// Creates the WebGraph if it does not already exist.
        /// </summary>
        public async Task EnsureGraphExistsAsync(Guid graphId, string userId, GraphOptions options)
        {
            var graph = await GetGraphAsync(graphId, userId);

            if (graph != null)
                return;

            await CreateGraphAsync(graphId, userId, options);
        }


        /// <summary>
        /// Maps page data to a Node and its relationships in the WebGraph.
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

            // Create the Node if it does not exist, or promote an existing Dummy Node
            // now that the page has been crawled and can be populated with real data.
            var node = await GetOrCreateNodeAsync(
                graphId, 
                pageData.Url, 
                NodeState.Populated);

            // Skip unnecessary updates unless this is the initial crawl
            // or the page content has changed.
            if (!ShouldUpdateNode(pageData, node, crawlDepth))
            {
                _logger.LogDebug(
                    "No update required for {Url} - content has not changed.", 
                    pageData.Url);

                return;
            }

            // Populate the Node with the latest normalised page data.
            await PopulateNodeAsync(node, pageData);

            // Rebuild the outgoing relationships from the latest crawl if Replace mode is set.
            if (_graphingSettings.WebGraph.OutgoingNodesUpdateMode == OutgoingNodesUpdateMode.Replace)
            {
                await ClearOutgoingRelationshipsAsync(graphId, node);
            }

            // A redirect is represented as a relationship from the original URL
            // to the final resolved URL.
            if (pageData.IsRedirect)
            {
                _logger.LogDebug("Handling redirect {OriginalUrl} -> {Url}",
                    pageData.OriginalUrl, 
                    pageData.Url);

                await MarkNodeAsRedirectedAsync(
                    graphId, 
                    pageData.OriginalUrl, 
                    pageData.Url);
            }

            // Map the hyperlinks discovered on the page to outgoing relationships.
            // Newly discovered target Nodes may need to be crawled next.
            var nodesToPopulate = await AddOutgoingRelationshipsAsync(
                graphId, 
                pageData, 
                crawlDepth);

            // Reload the Node because relationship updates may have changed
            // its stored state, such as its popularity score.
            node = await ReloadNodeAsync(node);
            if (node is null) 
                return;

            // Notify the caller that this Node has been populated.
            if (nodePopulatedCallback != null)
                await nodePopulatedCallback(node);

            // Request crawling of any related Nodes that require population.
            if (nodePopulationRequestCallback != null)
                await RequestNodePopulationAsync(
                    nodesToPopulate, 
                    nodePopulationRequestCallback, 
                    crawlDepth);
        }


        /// <summary>
        /// Retrieves an existing Node or creates a new Node for the supplied URL.
        /// </summary>
        protected async Task<Node> GetOrCreateNodeAsync(
            Guid graphId,
            string url,
            NodeState state = NodeState.Dummy)
        {
            if (string.IsNullOrEmpty(url)) 
                throw new ArgumentNullException(nameof(url));

            var node = await GetNodeAsync(graphId, url);

            // Create a new Node when the URL has not been seen before.
            // DIALOG: This is idempotency in practice — discovering the same URL
            // multiple times does not create duplicate Nodes.
            if (node == null)
            {
                _logger.LogDebug(
                    "Creating new node for {Url} with state {State}",
                    url,
                    state);

                node = new Node(graphId, url, state);
                await SaveNodeAsync(node);
            }

            // A Dummy Node represents a discovered link that has not yet been populated.
            // Promote it once the page has been crawled and real data is available.
            // DIALOG: This is eventual consistency in practice —
            // the graph converges as events continue to be processed.
            else if (state == NodeState.Populated &&
                node.State == NodeState.Dummy)
            {
                await MarkNodeAsPopulatedAsync(node);
            }

            return node;
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
            // Update Nodes that have not yet been fully populated.
            if (node.State != NodeState.Populated)
                return true;

            // The initial crawl always refreshes the Node.
            if (crawlDepth == 0)
                return true;

            // Update when the page content has changed.
            if (node.ContentFingerprint != pageData.ContentFingerprint)
                return true;

            // Update when the source reports a different modification time.
            if (node.SourceLastModified != pageData.SourceLastModified)
                return true;

            return false;
        }


        /// <summary>
        /// Populates a Node with normalised page data.
        /// </summary>
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
        /// Marks a Node as redirected and creates the relationship to its target Node.
        /// </summary>
        protected async Task MarkNodeAsRedirectedAsync(Guid graphId, string sourceUrl, string targetUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl)) 
                throw new ArgumentNullException(nameof(sourceUrl));
            
            if (string.IsNullOrEmpty(targetUrl)) 
                throw new ArgumentNullException(nameof(targetUrl));

            var sourceNode = await GetOrCreateNodeAsync(graphId, sourceUrl);
            var targetNode = await GetOrCreateNodeAsync(graphId, targetUrl);

            // Preserve Nodes that have already been populated with page data.
            if (sourceNode.State == NodeState.Populated)
            {
                _logger.LogDebug(
                    "Skipping redirect for {SourceUrl} – already populated.", 
                    sourceUrl);

                return;
            }

            // Represent the redirect on the source Node.
            sourceNode.State = NodeState.Redirected;
            sourceNode.RedirectedToUrl = targetUrl;

            // A redirect is also represented as a directional relationship
            // from the original URL to the final target URL.
            await AddOutgoingRelationshipAsync(
                graphId, 
                sourceNode, 
                targetNode);

            await AddIncomingRelationshipAsync(
                graphId, 
                targetNode, 
                sourceNode);

            _logger.LogDebug(
                "Marked node {SourceUrl} as redirected to {TargetUrl} and updated nodes.",
                sourceUrl, 
                targetUrl);

            // Relationship changes can affect Node popularity.
            sourceNode.PopularityScore = 
                await GetPopularityScoreAsync(graphId, sourceNode);

            targetNode.PopularityScore = 
                await GetPopularityScoreAsync(graphId, targetNode);

            await SaveNodeAsync(targetNode);
            await SaveNodeAsync(sourceNode);
        }


        /// <summary>
        /// Adds outgoing relationships discovered in the page data and returns the target Nodes to populate.
        /// </summary>
        private async Task<IEnumerable<Node>> AddOutgoingRelationshipsAsync(Guid graphId, PageData pageData, int crawlDepth)
        {
            var nodesToPopulate = new HashSet<Node>();

            // Map each discovered link to an outgoing graph relationship.
            foreach (var link in pageData.Links)
            {
                var targetNode = await AddNodeRelationshipAsync(
                    graphId,
                    pageData.Url,
                    link,
                    crawlDepth);

                if (targetNode != null)
                {
                    nodesToPopulate.Add(targetNode);
                }
            }

            return nodesToPopulate;
        }


        /// <summary>
        /// Reloads a Node from the WebGraph.
        /// </summary>
        private async Task<Node?> ReloadNodeAsync(Node node)
        {
            return await GetNodeAsync(node.GraphId, node.Url);
        }


        /// <summary>
        /// Requests population of Nodes that are permitted to be refreshed.
        /// </summary>
        private async Task RequestNodePopulationAsync(
            IEnumerable<Node> nodes,
            Func<Node, Task> onNodePopulationRequest,
            int crawlDepth)
        {
            foreach (var node in nodes)
            {
                // Skip Nodes that are still within their refresh throttle period.
                if (CanRefreshNode(node, crawlDepth))
                {
                    // Set the next time this Node is eligible to be refreshed
                    // before requesting its population.
                    node.AllowRefreshAfter = DateTimeOffset.UtcNow.AddSeconds(
                        _graphingSettings.WebGraph.NodeRefreshThrottleSeconds);

                    await SaveNodeAsync(node);

                    await onNodePopulationRequest(node);
                }
            }
        }


        /// <summary>
        /// Adds a directional relationship between two Nodes and returns the target Node when it should be populated.
        /// </summary>
        protected async Task<Node?> AddNodeRelationshipAsync(
            Guid graphId, 
            string sourceUrl, 
            string targetUrl, 
            int crawlDepth)
        {
            if (string.IsNullOrEmpty(sourceUrl)) 
                throw new ArgumentNullException(nameof(sourceUrl));

            if (string.IsNullOrEmpty(targetUrl)) 
                throw new ArgumentNullException(nameof(targetUrl));

            // Ignore links that point back to the same Node.
            if (sourceUrl == targetUrl)
                return null;

            // The source page has already been crawled, while the target may only
            // have been discovered as a link and therefore starts as a Dummy Node.
            var sourceNode = await GetOrCreateNodeAsync(
                graphId, 
                sourceUrl, 
                NodeState.Populated);

            var targetNode = await GetOrCreateNodeAsync(
                graphId, 
                targetUrl, 
                NodeState.Dummy);

            var relationshipAdded = await AddOutgoingRelationshipAsync(
                graphId, 
                sourceNode, 
                targetNode);

            // Include newly discovered relationships, or existing relationships
            // encountered again during the initial crawl.
            if (relationshipAdded || crawlDepth == 0)
            {
                _logger.LogDebug(
                    "Adding outgoing/incoming relationship from {Url} to {Url}",
                    sourceNode.Url, 
                    targetNode.Url);

                // Record the reverse view of the same directional relationship
                // so the target Node also knows which Nodes link to it.
                await AddIncomingRelationshipAsync(
                    graphId, 
                    targetNode, 
                    sourceNode);

                // Relationship changes can affect Node popularity.
                sourceNode.PopularityScore = 
                    await GetPopularityScoreAsync(graphId, sourceNode);

                targetNode.PopularityScore = 
                    await GetPopularityScoreAsync(graphId, targetNode);

                await SaveNodeAsync(sourceNode);
                await SaveNodeAsync(targetNode);

                return targetNode;
            }

            // Existing relationships do not need to trigger another population request.
            return null;
        }


        /// <summary>
        /// Determines whether a Node is permitted to be refreshed.
        /// </summary>
        protected bool CanRefreshNode(
            Node node, 
            int crawlDepth)
        {
            // Initial crawls always bypass the refresh throttle.
            if (crawlDepth == 0) 
                return true;

            // Recursive crawls may refresh the Node when no throttle has been set
            // or when the existing throttle period has expired.
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


        /// <summary>
        /// Promotes a dummy Node to a populated Node.
        /// </summary>
        protected async Task MarkNodeAsPopulatedAsync(Node node)
        {
            _logger.LogDebug(
                "Promoting dummy node {Url} to populated.", 
                node.Url);

            node.State = NodeState.Populated;

            await SaveNodeAsync(node);
        }


        /// <summary>
        /// Saves a Node and updates its modification timestamp.
        /// </summary>
        private async Task SaveNodeAsync(Node node)
        {
            node.ModifiedAt = DateTimeOffset.UtcNow;

            await SetNodeAsync(node);
        }


        /// <summary>
        /// Returns a diagnostic text view of the WebGraph to the specified traversal limits.
        /// </summary>
        public async Task<string> GetGraphDiagnosticViewAsync(
            Guid graphId,
            int maxDepth,
            int? maxNodes)
        {
            var sb = new System.Text.StringBuilder();

            try
            {
                // Get an initial populated Node to use as the starting point for traversal.
                var initialNodes = await GetInitialGraphNodes(graphId, 1);
                var startNode = initialNodes.FirstOrDefault();

                if (startNode == null)
                {
                    sb.AppendLine($"Graph {graphId} — No nodes found.");
                    return sb.ToString();
                }

                // Retrieve the surrounding graph within the requested traversal limits.
                var nodes = await GetNodeNeighborhoodAsync(
                    graphId,
                    startNode.Url,
                    maxDepth,
                    maxNodes);

                var nodeList = nodes.ToList();

                // Build a readable text representation of the retrieved Nodes.
                sb.AppendLine($"Graph {graphId} — Total Nodes: {nodeList.Count}");
                sb.AppendLine($"Neighborhood start: {startNode.Url}");

                foreach (var node in nodeList.OrderBy(n => n.Url))
                {
                    sb.AppendLine($"  Node: {node.Url}");
                    sb.AppendLine($"    State: {node.State}");
                    sb.AppendLine($"    Title: {node.Title}");
                    sb.AppendLine($"    Popularity: {node.PopularityScore}");
                    sb.AppendLine($"    Incoming: {node.IncomingNodes.Count} | Outgoing: {node.OutgoingNodes.Count}");

                    // Include the Node's outgoing relationships.
                    if (node.OutgoingNodes.Any())
                    {
                        sb.AppendLine("    Outgoing Links:");

                        foreach (var outNode in node.OutgoingNodes.OrderBy(n => n.Url))
                            sb.AppendLine(
                                $"      -> {outNode.Url} [{outNode.State}]");
                    }

                    // Include the Node's incoming relationships.
                    if (node.IncomingNodes.Any())
                    {
                        sb.AppendLine("    Incoming Links:");

                        foreach (var inNode in node.IncomingNodes.OrderBy(n => n.Url))
                            sb.AppendLine(
                                $"      <- {inNode.Url} [{inNode.State}]");
                    }
                }

                sb.AppendLine(new string('-', 50));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to dump graph contents for GraphId {GraphId}",
                    graphId);

                sb.AppendLine(
                    $"Error dumping graph {graphId}: {ex.Message}");
            }

            return sb.ToString();
        }







        // THESE WILL BE ADDED IN THE NEXT CHAPTER FOR THE API

        /// <summary>
        /// Retrieves the initial populated Nodes used to load a WebGraph.
        /// </summary>
        public abstract Task<IEnumerable<Node>> GetInitialGraphNodes(Guid graphId, int topN);

        /// <summary>
        /// Returns the total number of populated Nodes in a WebGraph.
        /// </summary>
        public abstract Task<long> TotalPopulatedNodesAsync(Guid graphID);

        /// <summary>
        /// Retrieves the Nodes surrounding a starting URL to the specified depth.
        /// </summary>
        public abstract Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);


        /// <summary>
        /// Retrieves a page of WebGraphs owned by a user.
        /// </summary>
        public abstract Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize, string userId);

        /// <summary>
        /// Updates an existing WebGraph owned by a user.
        /// </summary>
        public abstract Task<Graph> UpdateGraphAsync(Graph graph, string userId);

        /// <summary>
        /// Deletes a WebGraph owned by a user.
        /// </summary>
        public abstract Task<Graph?> DeleteGraphAsync(Guid graphId, string userId);




        // DO NOT INCLUDE IN DEMO - WORKING BUT NOT CURRENTLY BEING USED

        /// <summary>
        /// Removes Nodes that are no longer connected to the WebGraph.
        /// </summary>
        public abstract Task CleanupOrphanedNodesAsync(Guid graphId);

    }
}
