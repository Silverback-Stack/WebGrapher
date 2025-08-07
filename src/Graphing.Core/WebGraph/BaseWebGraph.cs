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

        public async Task AddWebPageAsync(WebPageItem page, Action<string> onLinkDiscovered)
        {
            _logger.LogDebug($"AddWebPageAsync started for {page.Url}");

            // Mark or promote URL as Populated
            var node = await GetOrCreateNodeAsync(page.GraphId, page.Url, NodeState.Populated);

            if (!HasPageChanged(page, node))
            {
                _logger.LogDebug($"No updated required for {page.Url} - content has not changed.");
                return;
            }

            node.SourceLastModified = page.SourceLastModified;
            await SetNodeAsync(node);

            // Clear existing links to replace them
            await ClearOutgoingLinksAsync(node);

            // Add new outgoing links (if any)
            foreach (var link in page.Links)
            {
                _logger.LogDebug($"Adding outgoing link from {page.Url} to {link}.");
                await AddLinkAsync(page.GraphId, page.Url, link, onLinkDiscovered);
            }

            // Handle redirect
            if (page.IsRedirect)
            {
                _logger.LogInformation($"Handling redirect {page.OriginalUrl} -> {page.Url}");
                await SetRedirectedAsync(page.GraphId, page.OriginalUrl, page.Url);
            }
        }

        protected async Task<Node> GetOrCreateNodeAsync(
            int graphId,
            string url,
            NodeState state = NodeState.Dummy)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            var node = await GetNodeAsync(graphId, url);
            if (node == null)
            {
                node = new Node(graphId, url, state);
                await SetNodeAsync(node);
            }
            else if (state == NodeState.Populated &&
                node.State == NodeState.Dummy)
            {
                await MarkNodeAsPopulatedAsync(node);
            }

            _logger?.LogDebug($"Creating new node for {url} with state: {state}");
            return node!;
        }

        protected async Task AddLinkAsync(int graphId, string fromUrl, string toUrl, Action<string> onLinkDiscovered)
        {
            if (fromUrl == toUrl)
                return; //ignore circular links to self

            if (string.IsNullOrEmpty(fromUrl)) throw new ArgumentNullException(nameof(fromUrl));
            if (string.IsNullOrEmpty(toUrl)) throw new ArgumentNullException(nameof(toUrl));

            var fromNode = await GetOrCreateNodeAsync(graphId, fromUrl, NodeState.Populated);
            var toNode = await GetOrCreateNodeAsync(graphId, toUrl, NodeState.Dummy);

            if (fromNode.OutgoingLinks.Add(toNode))
            {
                await SetNodeAsync(fromNode);

                if (onLinkDiscovered != null)
                {
                    await TryScheduleCrawlAsync(toNode, onLinkDiscovered);
                }
            }
        }

        protected async Task TryScheduleCrawlAsync(Node node, Action<string> onLinkDiscovered)
        {
            var now = DateTimeOffset.UtcNow;
            var backoff = TimeSpan.FromMinutes(SCHEDULE_THROTTLE_MINUTES);

            if (node.LastScheduledAt == null ||
                now >= node.LastScheduledAt.Value + backoff)
            {
                _logger.LogDebug($"Scheduling crawl for {node.Url} last scheduled: {node.LastScheduledAt}.");

                onLinkDiscovered?.Invoke(node.Url);
                node.LastScheduledAt = now;
                await SetNodeAsync(node);
            }
            else
            {
                var nextTime = node.LastScheduledAt?.AddMinutes(SCHEDULE_THROTTLE_MINUTES);
                _logger.LogDebug($"Scheduled crawl for {node.Url} throttled. Next eligible time: {nextTime}");
                //Discard policy - if node recently crawled then dont crawl again during the throttle period
            }
        }

        protected async Task MarkNodeAsPopulatedAsync(Node node)
        {
            _logger.LogDebug($"Promoting dummy node {node.Url} to populated.");

            node.State = NodeState.Populated;
            await SetNodeAsync(node);
        }

        protected async Task MarkNodeAsRedirectedAsync(Node node)
        {
            if (node.State == NodeState.Populated)
                throw new InvalidOperationException("Cannot change a populated node into a redirect node.");

            node.State = NodeState.Redirected;
            await SetNodeAsync(node);
        }

        protected async Task AddRedirectLinkAsync(int graphId, string fromUrl, string toUrl)
        {
            var fromNode = await GetOrCreateNodeAsync(graphId, fromUrl);
            var toNode = await GetOrCreateNodeAsync(graphId, toUrl);

            if (fromNode.State == NodeState.Populated)
                throw new InvalidOperationException("Cannot add redirect link from a populated node.");

            fromNode.OutgoingLinks.Add(toNode);
            await SetNodeAsync(fromNode);
        }

        protected async Task SetRedirectedAsync(int graphId, string fromUrl, string toUrl)
        {
            if (string.IsNullOrEmpty(fromUrl)) throw new ArgumentNullException(nameof(fromUrl));
            if (string.IsNullOrEmpty(toUrl)) throw new ArgumentNullException(nameof(toUrl));

            var fromNode = await GetOrCreateNodeAsync(graphId, fromUrl);

            if (fromNode.State == NodeState.Populated)
            {
                _logger.LogDebug($"Skipping redirect for {fromUrl} – already populated.");
                return;
            }

            // Mark as redirected if not already populated
            _logger.LogDebug($"Marked node {fromUrl} as redirected.");
            await MarkNodeAsRedirectedAsync(fromNode);

            // Create edge from original to destination
            _logger.LogDebug($"Added redirect link from {fromUrl} to {toUrl}");
            await AddRedirectLinkAsync(graphId, fromUrl, toUrl);
        }

        protected async Task ClearOutgoingLinksAsync(Node node)
        {
            _logger?.LogDebug($"Clearing outgoing links for node {node.Url}");
            node.OutgoingLinks.Clear();
            await SetNodeAsync(node);
        }

        private bool HasPageChanged(WebPageItem page, Node node)
        {
            if (node == null) return true;

            return node.State != NodeState.Populated ||
                node.SourceLastModified != page.SourceLastModified;
        }

        public abstract Task<Node?> GetNodeAsync(int graphId, string url);

        protected abstract Task<Node> SetNodeAsync(Node node);

        public abstract Task CleanupOrphanedNodesAsync(int graphId);

        public abstract Task<int> TotalPopulatedNodesAsync(int graphID);
    }
}
