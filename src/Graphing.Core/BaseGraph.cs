//using System;
//using Events.Core.Bus;
//using Events.Core.EventTypes;
//using Events.Core.Helpers;
//using Graphing.Core.Models;
//using Microsoft.Extensions.Logging;

//namespace Graphing.Core
//{
//    public abstract class BaseGraph : IGraph, IEventBusLifecycle
//    {
//        protected readonly ILogger _logger;
//        protected readonly IEventBus _eventBus;

//        private const int NODE_STALE_DAYS = 30;

//        protected BaseGraph(ILogger logger, IEventBus eventBus)
//        {
//            _logger = logger;
//            _eventBus = eventBus;
//        }

//        public void SubscribeAll()
//        {
//            _eventBus.Subscribe<GraphPageEvent>(UpdateGraph);
//        }

//        public void UnsubscribeAll()
//        {
//            _eventBus.Subscribe<GraphPageEvent>(UpdateGraph);
//        }

//        private async Task UpdateGraph(GraphPageEvent evt)
//        {
//            var originalUrl = evt.OriginalUrl.AbsoluteUri;
//            var url = evt.Url.AbsoluteUri;

//            _logger.LogDebug($"Original URL: {originalUrl}");
//            _logger.LogDebug($"URL: {url}");

//            if (evt.IsRedirect)
//            {
//                var originalNode = await GetNodeAsync(originalUrl);

//                if (originalNode is null)
//                    originalNode = new Node(originalUrl);

//                originalNode.SetRedirected(redirectedTo: url);

//                originalNode.AddRedirectEdge(
//                    target: url,
//                    redirectedFrom: originalUrl);

//                await SetNodeAsync(originalNode);

//                _logger.LogDebug($"Processed redirect node from {originalUrl} -> {url}");
//            }

//            var node = await GetNodeAsync(url);
//            var links = evt.Links.Select(l => l.AbsoluteUri);

//            if (node is null)
//            {
//                node = new Node(
//                    url,
//                    evt.Title,
//                    evt.Keywords,
//                    evt.SourceLastModified,
//                    links);

//                _logger.LogDebug($"Created new node for {url}");
//            }
//            else if (node.State == NodeState.Dummy ||
//                (node.State == NodeState.Populated && node.IsStale(NODE_STALE_DAYS)))
//            {
//                node.SetPopulated(
//                    evt.Title,
//                    evt.Keywords,
//                    evt.SourceLastModified,
//                    links);

//                _logger.LogDebug($"Updated stale or dummy node for {url}");
//            } 
//            else
//            {
//                _logger.LogDebug($"Node for {url} is already fresh and populated.");
//                return;
//            }

//            await SetNodeAsync(node);
//            await FollowEdges(evt, node);
//        }

//        private async Task FollowEdges(GraphPageEvent evt, Node node)
//        {
//            foreach (var edge in node.Edges)
//            {
//                if (edge.Id == "https://www.bing.com/chat")
//                {
//                    var stop = true;
//                }

//                var edgeNode = await GetNodeAsync(edge.Id);

//                if (edgeNode is not null 
//                    && edgeNode.State == NodeState.Redirected)
//                {
//                    _logger.LogDebug($"Skipping redirected edge {edge.Id}");
//                    continue;
//                }

//                if (edgeNode is not null
//                    && edgeNode.State == NodeState.Populated
//                    && !edgeNode.IsStale(NODE_STALE_DAYS))
//                {
//                    _logger.LogDebug($"Skipping fresh or populated edge {edge.Id}");
//                    continue;
//                }

//                if (edgeNode is not null && node.State == NodeState.Dummy && (DateTimeOffset.UtcNow - node.CreatedAt) < TimeSpan.FromMinutes(10))
//                {
//                    _logger.LogDebug($"Skipping dummy node recently discovered: {node.Id}");
//                    continue;
//                }

//                if (edgeNode is null || edgeNode.State == NodeState.Dummy)
//                {
//                    var depth = evt.CrawlPageEvent.Depth + 1;

//                    var crawlPageEvent = new CrawlPageEvent(
//                        evt.CrawlPageEvent,
//                        new Uri(edge.Id),
//                        attempt: 1,
//                        depth);

//                    var scheduledOffset = EventScheduleHelper.AddRandomDelayTo(DateTimeOffset.UtcNow);

//                    _logger.LogDebug($"Scheduling crawl for edge {edge.Id} at depth {depth}");

//                    await _eventBus.PublishAsync(
//                        crawlPageEvent, priority: depth, scheduledOffset);                    
//                }
//            }
//        }

//        public abstract IGraphAnalyser GraphAnalyser { get; }

//        public abstract Task<Node?> GetNodeAsync(string id);

//        public abstract Task<Node?> SetNodeAsync(Node node);

//        public abstract Task DeleteNodeAsync(string id);

//        public abstract void Dispose();
//    }
//}
