using System;
using System.Net;
using System.Reflection.Metadata;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Events.Core.Bus;
using Events.Core.Types;
using Graphing.Core.Exporters;
using Logging.Core;

namespace Graphing.Core
{
    public class MemoryGraph : IGraph
    {
        private readonly IAppLogger _appLogger;
        private readonly IEventBus _eventBus;
        private readonly IGraphAnalyser _graphAnalyser;
        private readonly Dictionary<string, Node> _nodes;

        public MemoryGraph(IAppLogger appLogger, IEventBus eventBus)
        {
            _appLogger = appLogger;
            _eventBus = eventBus;
            _nodes = new Dictionary<string, Node>();
            _graphAnalyser = new MemoryGraphAnalyser(_nodes);
        }

        public async Task StartAsync()
        {
            await _eventBus.StartAsync();

            _eventBus.Subscribe<GraphPageEvent>(async evt =>
            {
                await HandleEvent(evt);
                await Task.CompletedTask;
            });
        }

        public async Task StopAsync()
        {
            await _eventBus.StopAsync();
        }

        public void Dispose()
        {
            _eventBus.Dispose();
        }

        private async Task HandleEvent(GraphPageEvent evt)
        {
            if (_graphAnalyser.TotalNodes() > 100 && _graphAnalyser.TotalEdges() > 100)
            {
                var gexfExporter = new GexfGraphExporter();
                var data = gexfExporter.Export(_nodes);
            }

            var node = AddNode(
                        evt.CrawlPageEvent.Url.AbsoluteUri,
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
                    var crawlPageEvent = new CrawlPageEvent(
                        evt.CrawlPageEvent, 
                        edgeUri, 
                        evt.CrawlPageEvent.Depth++);

                    await _eventBus.PublishAsync(crawlPageEvent);
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

        public Node AddNode(string key, string title, string keywords, DateTimeOffset sourceLastModified, IEnumerable<string> edges)
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
    }
}
