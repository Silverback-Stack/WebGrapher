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
            _eventBus.Subscribe<GraphPageEvent>(EventHandler);
        }

        public void UnsubscribeAll()
        {
            _eventBus.Subscribe<GraphPageEvent>(EventHandler);
        }

        private async Task EventHandler(GraphPageEvent evt)
        {
            await UpdateGraph(evt);
        }

        private async Task UpdateGraph(GraphPageEvent evt)
        {
            if (evt.IsRedirect)
            {
                var redirectedNode = await GetNodeAsync(evt.RequestUrl.AbsoluteUri);

                if (redirectedNode is null)
                    redirectedNode = new Node(evt.RequestUrl.AbsoluteUri);

                redirectedNode.SetRedirected(redirectedTo: evt.ResolvedUrl.AbsoluteUri);

                redirectedNode.AddRedirectEdge(
                    target: evt.ResolvedUrl.AbsoluteUri,
                    redirectedFrom: evt.RequestUrl.AbsoluteUri);

                await SetNodeAsync(redirectedNode);
            }

            var resolvedNode = await GetNodeAsync(evt.ResolvedUrl.AbsoluteUri);
            var links = evt.Links.Select(l => l.AbsoluteUri);

            if (resolvedNode is null)
            {
                resolvedNode = new Node(
                    evt.ResolvedUrl.AbsoluteUri,
                    evt.Title,
                    evt.Keywords,
                    evt.SourceLastModified,
                    links);
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
            } 
            else
            {
                //already fresh and populated
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
                    continue;

                if (edgeNode is not null
                    && edgeNode.State == NodeState.Populated
                    && !edgeNode.IsStale(NODE_STALE_DAYS))
                    continue;

                if (edgeNode is null || edgeNode.State == NodeState.Dummy)
                {
                    var depth = evt.CrawlPageEvent.Depth + 1;

                    var crawlPageEvent = new CrawlPageEvent(
                        evt.CrawlPageEvent,
                        new Uri(edge.Id),
                        attempt: 1,
                        depth);

                    var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);
                    await _eventBus.PublishAsync(crawlPageEvent, scheduledOffset);
                    _logger.LogWarning($"Published crawl page event for edge: {edge.Id} depth: {depth}");
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
