using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.Version2
{
    public abstract class BaseWebGraph : IWebGraph, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;

        private const int SCHEDULE_THROTTLE_MINUTES = 15;

        protected BaseWebGraph(ILogger logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }
        public void SubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(ProcessGraphPageEventAsync);
        }

        private async Task ProcessGraphPageEventAsync(GraphPageEvent evt)
        {
            var webPage = new WebPageItem
            {
                Url = evt.Url.AbsoluteUri,
                OriginalUrl = evt.OriginalUrl.AbsoluteUri,
                IsRedirect = evt.IsRedirect,
                Links = evt.Links != null ? evt.Links.Select(l => l.AbsoluteUri) : new List<string>(),
                SourceLastModified = evt.SourceLastModified
            };

            //setup event delegate
            Action<string> onLinkDiscovered = (url) =>
            {
                var depth = evt.CrawlPageEvent.Depth + 1;

                var crawlPageEvent = new CrawlPageEvent(
                    evt.CrawlPageEvent,
                    new Uri(url),
                    attempt: 1,
                    depth);

                var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);

                _logger.LogDebug($"Scheduling crawl for link {url} at depth {depth}");

                _eventBus.PublishAsync(crawlPageEvent, priority: depth, scheduledOffset);
            };

            await AddWebPageAsync(webPage, onLinkDiscovered);
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

        protected async Task<WebGraphNode> GetOrCreateNodeAsync(
            int graphId,
            string url,
            NodeState state = NodeState.Dummy)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            var node = await GetNodeAsync(graphId, url);
            if (node == null)
            {
                node = new WebGraphNode(graphId, url, state);
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

        protected async Task TryScheduleCrawlAsync(WebGraphNode node, Action<string> onLinkDiscovered)
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

        protected async Task MarkNodeAsPopulatedAsync(WebGraphNode node)
        {
            _logger.LogDebug($"Promoting dummy node {node.Url} to populated.");

            node.State = NodeState.Populated;
            await SetNodeAsync(node);
        }

        protected async Task MarkNodeAsRedirectedAsync(WebGraphNode node)
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

        protected async Task ClearOutgoingLinksAsync(WebGraphNode node)
        {
            _logger?.LogDebug($"Clearing outgoing links for node {node.Url}");
            node.OutgoingLinks.Clear();
            await SetNodeAsync(node);
        }

        private bool HasPageChanged(WebPageItem page, WebGraphNode node)
        {
            if (node == null) return true;

            return (node.State != NodeState.Populated ||
                node.SourceLastModified != page.SourceLastModified);
        }

        public abstract Task<WebGraphNode?> GetNodeAsync(int graphId, string url);

        protected abstract Task<WebGraphNode> SetNodeAsync(WebGraphNode node);

        public abstract Task CleanupOrphanedNodesAsync(int graphId);

        public abstract Task<int> TotalPopulatedNodesAsync(int graphID);

    }
}
