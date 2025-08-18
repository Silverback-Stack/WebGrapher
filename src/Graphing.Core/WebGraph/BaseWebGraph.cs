using System;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.WebGraph
{
    public abstract class BaseWebGraph : IWebGraph
    {
        protected readonly ILogger _logger;

        private const int SCHEDULE_THROTTLE_MINUTES = 15;

        protected BaseWebGraph(ILogger logger)
        {
            _logger = logger;
        }

        public async Task AddWebPageAsync(
            WebPageItem webPage, 
            Func<Node, Task> onNodePopulated, 
            Func<Node, Task> onLinkDiscovered,
            LinkUpdateMode linkUpdateMode = LinkUpdateMode.Append)
        {
            _logger.LogDebug($"AddWebPageAsync started for {webPage.Url}");

            // Mark or promote URL as Populated
            var node = await GetOrCreateNodeAsync(webPage.GraphId, webPage.Url, NodeState.Populated);

            if (!HasPageChanged(webPage, node))
            {
                _logger.LogDebug($"No updated required for {webPage.Url} - content has not changed.");
                return;
            }

            await PopulateNodeFromWebPageAsync(node, webPage);

            if (linkUpdateMode == LinkUpdateMode.Replace)
            {
                await ClearOutgoingLinksAsync(node);
            }

            if (webPage.IsRedirect)
            {
                _logger.LogInformation($"Handling redirect {webPage.OriginalUrl} -> {webPage.Url}");
                await SetRedirectedAsync(webPage.GraphId, webPage.OriginalUrl, webPage.Url);
            }

            var addedLinks = await AddLinksAsync(webPage);

            if (onNodePopulated != null)
            {
                await onNodePopulated(node);
            }

            if (onLinkDiscovered != null)
            {
                await ScheduleAddedLinksAsync(addedLinks, onLinkDiscovered);
            }
        }

        private async Task<IEnumerable<Node>> AddLinksAsync(WebPageItem webPage)
        {
            var addedLinks = new HashSet<Node>();

            // Add new outgoing links (if any)
            foreach (var link in webPage.Links)
            {
                var linkedNode = await AddLinkAsync(webPage.GraphId, webPage.Url, link);
                if (linkedNode != null)
                {
                    addedLinks.Add(linkedNode);
                }
            }

            return addedLinks;
        }

        private async Task ScheduleAddedLinksAsync(IEnumerable<Node> addedLinks, Func<Node, Task> onLinkDiscovered)
        {
            foreach (var link in addedLinks)
            {
                if (CanScheduleCrawl(link))
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
            await SaveNodeAsync(node);
        }

        /// <summary>
        /// Returns target Node if a link was added.
        /// </summary>
        protected async Task<Node?> AddLinkAsync(Guid graphId, string fromUrl, string toUrl)
        {
            if (fromUrl == toUrl)
                return null; //ignore circular links to self

            if (string.IsNullOrEmpty(fromUrl)) throw new ArgumentNullException(nameof(fromUrl));
            if (string.IsNullOrEmpty(toUrl)) throw new ArgumentNullException(nameof(toUrl));

            var fromNode = await GetOrCreateNodeAsync(graphId, fromUrl, NodeState.Populated);
            var toNode = await GetOrCreateNodeAsync(graphId, toUrl, NodeState.Dummy);

            if (fromNode.OutgoingLinks.Add(toNode))
            {
                _logger.LogDebug($"Adding outgoing/incoming links from {fromNode.Url} to {toNode.Url}.");

                toNode.IncomingLinks.Add(fromNode);

                await SaveNodeAsync(fromNode);
                await SaveNodeAsync(toNode);

                return toNode;
            }

            //no link added
            return null;
        }

        /// <summary>
        /// Returns True if a Node can be scheduled for a crawl.
        /// </summary>
        protected bool CanScheduleCrawl(Node node)
        {
            var now = DateTimeOffset.UtcNow;
            var backoff = TimeSpan.FromMinutes(SCHEDULE_THROTTLE_MINUTES);

            if (node.LastScheduledAt == null ||
                now >= node.LastScheduledAt.Value + backoff)
            {
                return true;
            }

            //Discard policy: node recently crawled - dont crawl again during the throttle period
            var nextTime = node.LastScheduledAt?.AddMinutes(SCHEDULE_THROTTLE_MINUTES);
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
            fromNode.OutgoingLinks.Add(toNode);
            toNode.IncomingLinks.Add(fromNode);
            
            _logger.LogDebug($"Marked node {fromUrl} as redirected to {toUrl} and updated links.");

            await SaveNodeAsync(toNode);
            await SaveNodeAsync(fromNode);
        }

        protected async Task ClearOutgoingLinksAsync(Node node)
        {
            _logger?.LogDebug($"Clearing outgoing links for node {node.Url}");

            foreach (var target in node.OutgoingLinks)
            {
                target.IncomingLinks.Remove(node);
                await SaveNodeAsync(target);
            }

            node.OutgoingLinks.Clear();
            await SaveNodeAsync(node);
        }

        private bool HasPageChanged(WebPageItem webPage, Node node)
        {
            if (node == null) return true;

            return node.State != NodeState.Populated ||
                node.SourceLastModified != webPage.SourceLastModified;
        }

        private async Task SaveNodeAsync(Node node)
        {
            node.ModifiedAt = DateTimeOffset.UtcNow;
            await SetNodeAsync(node);
        }


        public abstract Task<Node?> GetNodeAsync(Guid graphId, string url);

        public abstract Task<Node> SetNodeAsync(Node node);

        public abstract Task<int> TotalPopulatedNodesAsync(Guid graphID);

        public abstract Task CleanupOrphanedNodesAsync(Guid graphId);

        public abstract Task<IEnumerable<Node>> TraverseGraphAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null);



        public abstract Task<Graph?> GetGraphAsync(Guid graphId);

        public abstract Task<Graph> CreateGraphAsync(
            string name, 
            string description, 
            Uri url, 
            int maxDepth, 
            int maxLinks, 
            bool followExternalLinks, 
            bool excludeQueryStrings, 
            string urlMatchRegex, 
            string titleElementXPath, 
            string contentElementXPath, 
            string summaryElementXPath, 
            string imageElementXPath, 
            string relatedLinksElementXPath);

        public abstract Task<Graph> UpdateGraphAsync(Graph graph);

        public abstract Task<Graph?> DeleteGraphAsync(Guid graphId);

        public abstract Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize);

    }
}
