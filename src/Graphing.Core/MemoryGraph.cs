using System;
using Events.Core.Bus;
using Events.Core.EventTypes;
using Graphing.Core.Exporters;
using Logging.Core;

namespace Graphing.Core
{
    public class MemoryGraph : IGraph, IEventBusLifecycle
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IGraphAnalyser _graphAnalyser;
        private readonly Dictionary<string, Node> _nodes;

        public MemoryGraph(ILogger logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
            _nodes = new Dictionary<string, Node>();
            _graphAnalyser = new MemoryGraphAnalyser(_nodes);
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
            if (_graphAnalyser.TotalNodes() > 100 && _graphAnalyser.TotalEdges() > 100)
            {
                var gexfExporter = new GexfGraphExporter();
                var data = gexfExporter.Export(_nodes);
            }

            var node = AddNode(
                        evt.Url.AbsoluteUri,
                        evt.Title,
                        evt.Keywords,
                        evt.SourceLastModified,
                        evt.Links);

            await FollowEdges(evt, node);
        }

        private async Task FollowEdges(GraphPageEvent evt, Node node)
        {
            foreach (var edge in node.Edges)
            {
                if (!Uri.TryCreate(edge, UriKind.Absolute, out var edgeUri))
                    continue;

                if (!IsNodePopulated(edge))
                {
                    var depth = evt.CrawlPageEvent.Depth + 1;

                    var crawlPageEvent = new CrawlPageEvent(
                        evt.CrawlPageEvent, 
                        edgeUri,
                        attempt: 1,
                        depth);

                    //|TODO: extract to a utlility function in Shared.Requests
                    //use here and also for scrapepageerrors scheduling
                    var delaySeconds = new Random().Next(5);

                    await _eventBus.PublishAsync(crawlPageEvent, DateTimeOffset.UtcNow.AddSeconds(delaySeconds));
                }
            }
        }
        public IGraphAnalyser GraphAnalyser => _graphAnalyser;

        public IReadOnlyDictionary<string, Node> GetNodes() => _nodes;

        /// <summary>
        /// Returns if a node is populated with data.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool IsNodePopulated(string edge)
        {
            if (_nodes.TryGetValue(edge, out var node))
                return node.HasData;

            return false;
        }

        public Node AddNode(string key, string title, string keywords, DateTimeOffset? sourceLastModified, IEnumerable<string> edges)
        {
            if (_nodes.TryGetValue(key, out var node))
            {
                node.Title = title;
                node.Keywords = keywords;
                node.SourceLastModifed = sourceLastModified;
                node.SetEdges(edges);
            }
            else
            {
                node = new Node(key, title, keywords, sourceLastModified, edges);
            }

            foreach (var edge in node.Edges) {
                if (!_nodes.ContainsKey(edge))
                    _nodes[edge] = new Node(edge); //add placeholder node
            }

            _nodes[key] = node;
            return node;
        }

        public void RemoveNode(string url)
        {
            _nodes.Remove(url);
        }
        public void Dispose()
        {
            _logger.LogInformation($"Disposing: {typeof(MemoryGraph).Name}, memory cleared.");
            _nodes.Clear();
        }
    }
}
