using System;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.WebGraph
{
    public abstract class BaseWebGraph : IWebGraph
    {
        protected readonly ILogger _logger;
        protected readonly GraphingSettings _graphingSettings;
        
        protected BaseWebGraph(ILogger logger, GraphingSettings graphingSettings)
        {
            _logger = logger;
            _graphingSettings = graphingSettings;
        }

        public async Task AddWebPageAsync(
            WebPageItem webPage, 
            bool forceRefresh,
            Func<Node, Task> nodePopulatedCallback, 
            Func<Node, Task> linkDiscoveredCallback,
            NodeEdgesUpdateMode linkUpdateMode = NodeEdgesUpdateMode.Append)
        {
            _logger.LogDebug($"AddWebPageAsync started for {webPage.Url}");

            // Mark or promote URL as Populated
            var node = await GetOrCreateNodeAsync(webPage.GraphId, webPage.Url, NodeState.Populated);

            if (!HasPageChanged(webPage, node, forceRefresh))
            {
                _logger.LogDebug($"No updated required for {webPage.Url} - content has not changed.");
                return;
            }

            await PopulateNodeFromWebPageAsync(node, webPage);

            if (linkUpdateMode == NodeEdgesUpdateMode.Replace)
            {
                await ClearOutgoingLinksAsync(webPage.GraphId, node);
            }

            if (webPage.IsRedirect)
            {
                _logger.LogDebug($"Handling redirect {webPage.OriginalUrl} -> {webPage.Url}");
                await SetRedirectedAsync(webPage.GraphId, webPage.OriginalUrl, webPage.Url);
            }

            var addedLinks = await AddLinksAsync(webPage, forceRefresh);

            //fetch updated node with added links from data source
            node = await GetNodeAsync(webPage.GraphId, node.Url);
            if (node is null) return;

            if (nodePopulatedCallback != null)
            {
                await nodePopulatedCallback(node);
            }

            if (linkDiscoveredCallback != null)
            {
                await ScheduleAddedLinksAsync(addedLinks, linkDiscoveredCallback, forceRefresh);
            }
        }

        private async Task<IEnumerable<Node>> AddLinksAsync(WebPageItem webPage, bool forceRefresh)
        {
            var addedLinks = new HashSet<Node>();

            // Add new outgoing links (if any)
            foreach (var link in webPage.Links)
            {
                var linkedNode = await AddLinkAsync(webPage.GraphId, webPage.Url, link, forceRefresh);
                if (linkedNode != null)
                {
                    addedLinks.Add(linkedNode);
                }
            }

            return addedLinks;
        }

        private async Task ScheduleAddedLinksAsync(IEnumerable<Node> addedLinks, Func<Node, Task> onLinkDiscovered, bool forceRefresh)
        {
            foreach (var link in addedLinks)
            {
                //override CanScheduleCrawl if it's a user-initiated request
                var canScheduleCrawl = forceRefresh || CanScheduleCrawl(link);

                if (canScheduleCrawl)
                {
                    link.LastScheduledAt = DateTimeOffset.UtcNow;
                    await SaveNodeAsync(link);

                    await onLinkDiscovered(link);
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
                node = new Node(graphId, url, state);
                await SaveNodeAsync(node);
            }
            else if (state == NodeState.Populated &&
                node.State == NodeState.Dummy)
            {
                await MarkNodeAsPopulatedAsync(node);
            }

            _logger?.LogDebug($"Creating new node for {url} with state: {state}");
            return node!;
        }

        protected async Task PopulateNodeFromWebPageAsync(Node node, WebPageItem webPage)
        {
            node.Title = webPage.Title ?? string.Empty;
            node.Summary = webPage.Summary ?? string.Empty;
            node.ImageUrl = webPage.ImageUrl ?? string.Empty;
            node.Keywords = webPage.Keywords ?? string.Empty;
            node.Tags = webPage.Tags ?? Enumerable.Empty<string>();
            node.SourceLastModified = webPage.SourceLastModified;
            node.ContentFingerprint = webPage.ContentFingerprint;
            await SaveNodeAsync(node);
        }

        /// <summary>
        /// Returns target Node if a link was added.
        /// </summary>
        protected async Task<Node?> AddLinkAsync(Guid graphId, string fromUrl, string toUrl, bool forceRefresh)
        {
            if (fromUrl == toUrl)
                return null; //ignore circular links to self

            if (string.IsNullOrEmpty(fromUrl)) throw new ArgumentNullException(nameof(fromUrl));
            if (string.IsNullOrEmpty(toUrl)) throw new ArgumentNullException(nameof(toUrl));

            var fromNode = await GetOrCreateNodeAsync(graphId, fromUrl, NodeState.Populated);
            var toNode = await GetOrCreateNodeAsync(graphId, toUrl, NodeState.Dummy);

            var linkAdded = await AddOutgoingLinkAsync(graphId, fromNode, toNode);

            //if the link was added (didnt already exist)
            //or link exists but this is a user-initiated request (forces refresh of data)
            if (linkAdded || forceRefresh)
            {
                _logger.LogDebug($"Adding outgoing/incoming links from {fromNode.Url} to {toNode.Url}.");

                await AddIncomingLinkAsync(graphId, toNode, fromNode);

                // Update popularity scores
                fromNode.PopularityScore = await GetPopularityScoreAsync(graphId, fromNode);
                toNode.PopularityScore = await GetPopularityScoreAsync(graphId, toNode);

                await SaveNodeAsync(fromNode);
                await SaveNodeAsync(toNode);

                return toNode;
            }

            //link already exists
            return null;
        }

        /// <summary>
        /// Returns True if a Node can be scheduled for a crawl.
        /// </summary>
        protected bool CanScheduleCrawl(Node node)
        {
            var now = DateTimeOffset.UtcNow;
            var backoff = TimeSpan.FromSeconds(_graphingSettings.WebGraph.ScheduleCrawlThrottleSeconds);

            if (node.LastScheduledAt == null ||
                now >= node.LastScheduledAt.Value + backoff)
            {
                return true;
            }

            //Discard policy: node recently crawled - dont crawl again during the throttle period
            var nextTime = node.LastScheduledAt?.AddSeconds(_graphingSettings.WebGraph.ScheduleCrawlThrottleSeconds);
            _logger.LogDebug($"Scheduled crawl for {node.Url} throttled. Next eligible time: {nextTime}");
            return false;
        }


        protected async Task MarkNodeAsPopulatedAsync(Node node)
        {
            _logger.LogDebug($"Promoting dummy node {node.Url} to populated.");

            node.State = NodeState.Populated;
            await SaveNodeAsync(node);
        }

        protected async Task SetRedirectedAsync(Guid graphId, string fromUrl, string toUrl)
        {
            if (string.IsNullOrEmpty(fromUrl)) throw new ArgumentNullException(nameof(fromUrl));
            if (string.IsNullOrEmpty(toUrl)) throw new ArgumentNullException(nameof(toUrl));

            var fromNode = await GetOrCreateNodeAsync(graphId, fromUrl);
            var toNode = await GetOrCreateNodeAsync(graphId, toUrl);

            if (fromNode.State == NodeState.Populated)
            {
                _logger.LogDebug($"Skipping redirect for {fromUrl} – already populated.");
                return;
            }

            // Mark as redirected
            fromNode.State = NodeState.Redirected;
            fromNode.RedirectedToUrl = toUrl;

            // Maintain incoming/outgoing links
            var outgoingAdded = await AddOutgoingLinkAsync(graphId, fromNode, toNode);
            var incomingAdded = await AddIncomingLinkAsync(graphId, toNode, fromNode);

            _logger.LogDebug($"Marked node {fromUrl} as redirected to {toUrl} and updated links.");

            // Update popularity
            fromNode.PopularityScore = await GetPopularityScoreAsync(graphId, fromNode);
            toNode.PopularityScore = await GetPopularityScoreAsync(graphId, toNode);

            await SaveNodeAsync(toNode);
            await SaveNodeAsync(fromNode);
        }

        /// <summary>
        /// Determines whether a node needs to be updated based on the current state of the associated web page.
        /// </summary>
        /// <param name="forceRefresh">
        /// A flag indicating whether the check was triggered by an explicit user action,
        /// which should force a refresh regardless of other conditions.
        /// </param>
        private bool HasPageChanged(WebPageItem webPage, Node node, bool forceRefresh)
        {
            if (node == null) return true;

            return node.State != NodeState.Populated ||
               forceRefresh ||
               node.ContentFingerprint != webPage.ContentFingerprint ||
               node.SourceLastModified != webPage.SourceLastModified;
        }

        /// <summary>
        /// Persists any changes to Node and sets ModifiedAt to now.
        /// </summary>
        private async Task SaveNodeAsync(Node node)
        {
            node.ModifiedAt = DateTimeOffset.UtcNow;
            await SetNodeAsync(node);
        }


        //Graph Abstrations
        public abstract Task<Graph?> GetGraphAsync(Guid graphId);

        public abstract Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize);

        public abstract Task<Graph> CreateGraphAsync(GraphOptions options);

        public abstract Task<Graph?> UpdateGraphAsync(Graph graph);

        public abstract Task<Graph?> DeleteGraphAsync(Guid graphId);


        //Node Abstractions
        public abstract Task<Node?> GetNodeAsync(Guid graphId, string url);

        public abstract Task<Node> SetNodeAsync(Node node);

        protected abstract Task<bool> AddOutgoingLinkAsync(Guid graphId, Node fromNode, Node toNode);
        
        protected abstract Task<bool> AddIncomingLinkAsync(Guid graphId, Node toNode, Node fromNode);
        
        protected abstract Task ClearOutgoingLinksAsync(Guid graphId, Node node);

        public abstract Task CleanupOrphanedNodesAsync(Guid graphId);

        protected abstract Task<int> GetPopularityScoreAsync(Guid graphId, Node node);

        public abstract Task<IEnumerable<Node>> GetInitialGraphNodes(Guid graphId, int topN);

        public abstract Task<long> TotalPopulatedNodesAsync(Guid graphID);

        public abstract Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);

    }
}
