using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Events.Core.Helpers;
using Graphing.Core.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core
{
    public abstract class BaseGraph : IGraph, IEventBusLifecycle
    {
        protected readonly ILogger _logger;
        protected readonly IEventBus _eventBus;

        private const int NODE_STALE_DAYS = 30;

        protected BaseGraph(ILogger logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public void SubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(UpdateGraph);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(UpdateGraph);
        }

        private async Task UpdateGraph(GraphPageEvent evt)
        {
            var originalUrl = evt.CrawlPageEvent.Url.AbsoluteUri;
            var redirectedUrl = evt.Url.AbsoluteUri;

            if (evt.IsRedirect)
            {
                var node = await GetNodeAsync(originalUrl);

                if (node is null)
                    node = new Node(originalUrl);

                node.SetRedirected(redirectedTo: redirectedUrl);

                node.AddRedirectEdge(
                    target: redirectedUrl,
                    redirectedFrom: originalUrl);

                await SetNodeAsync(node);

                _logger.LogDebug($"Processed redirect node from {originalUrl} -> {redirectedUrl}");
            }

            var resolvedNode = await GetNodeAsync(redirectedUrl);
            var links = evt.Links.Select(l => l.AbsoluteUri);

            if (resolvedNode is null)
            {
                resolvedNode = new Node(
                    redirectedUrl,
                    evt.Title,
                    evt.Keywords,
                    evt.SourceLastModified,
                    links);

                _logger.LogDebug($"Created new node for {redirectedUrl}");
            }
            else if (resolvedNode.State == NodeState.Dummy ||
                (resolvedNode.State == NodeState.Populated 
                    && resolvedNode.IsStale(NODE_STALE_DAYS)))
            {
                resolvedNode.SetPopulated(
                    evt.Title,
                    evt.Keywords,
                    evt.SourceLastModified,
                    links);

                _logger.LogDebug($"Updated stale or dummy node for {redirectedUrl}");
            } 
            else
            {
                _logger.LogDebug($"Node for {redirectedUrl} is already fresh and populated.");
                return;
            }

            await SetNodeAsync(resolvedNode);
            await FollowEdges(evt, resolvedNode);
        }

        private async Task FollowEdges(GraphPageEvent evt, Node node)
        {
            foreach (var edge in node.Edges)
            {
                var edgeNode = await GetNodeAsync(edge.Id);

                if (edgeNode is not null 
                    && edgeNode.State == NodeState.Redirected)
                {
                    _logger.LogDebug($"Skipping redirected edge {edge.Id}");
                    continue;
                }

                if (edgeNode is not null
                    && edgeNode.State == NodeState.Populated
                    && !edgeNode.IsStale(NODE_STALE_DAYS))
                {
                    _logger.LogDebug($"Skipping fresh or populated edge {edge.Id}");
                    continue;
                }

                if (edgeNode is null || edgeNode.State == NodeState.Dummy)
                {
                    var depth = evt.CrawlPageEvent.Depth + 1;

                    var crawlPageEvent = new CrawlPageEvent(
                        evt.CrawlPageEvent,
                        new Uri(edge.Id),
                        attempt: 1,
                        depth);

                    var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);

                    await _eventBus.PublishAsync(
                        crawlPageEvent, priority: depth, scheduledOffset);
                    
                    _logger.LogDebug($"Scheduled crawl for edge {edge.Id} at depth {depth}");
                }
            }
        }

        public abstract IGraphAnalyser GraphAnalyser { get; }

        public abstract Task<Node?> GetNodeAsync(string id);

        public abstract Task<Node?> SetNodeAsync(Node node);

        public abstract Task DeleteNodeAsync(string id);

        public abstract void Dispose();
    }
}
