using System;
using Events.Core.Bus;
using Graphing.Core.Exporters;
using Graphing.Core.Models;
using Logging.Core;

namespace Graphing.Core.Adapters.Memory
{
    public class MemoryGraphAdapter : BaseGraph
    {
        private readonly IGraphAnalyser _graphAnalyser;
        private readonly Dictionary<string, Node> _nodes;

        public MemoryGraphAdapter(ILogger logger, IEventBus eventBus) : base(logger, eventBus)
        {
            _nodes = new Dictionary<string, Node>();
            _graphAnalyser = new MemoryGraphAnalyserAdapter(_nodes);
        }

        public IReadOnlyDictionary<string, Node> GetNodes() => _nodes;

        public override IGraphAnalyser GraphAnalyser => _graphAnalyser;


        /// <summary>
        /// Returns if a node is populated with data.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public override bool IsNodePopulated(string edge)
        {
            if (_nodes.TryGetValue(edge, out var node))
                return node.HasData;

            return false;
        }

        public override Node AddNode(string key, string title, string keywords, DateTimeOffset? sourceLastModified, IEnumerable<string> edges)
        {
            //TODO: TEMP CODE
            //REMOVE THIS AND IMPLEMENT EXPORT OF DATA
            ExportData();


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

            foreach (var edge in node.Edges)
            {
                if (!_nodes.ContainsKey(edge))
                    _nodes[edge] = new Node(edge); //add placeholder node
            }

            _nodes[key] = node;
            return node;
        }

        public override void RemoveNode(string url)
        {
            _nodes.Remove(url);
        }

        public override void Dispose()
        {
            _logger.LogInformation($"Disposing: {typeof(MemoryGraphAdapter).Name}, memory cleared.");
            _nodes.Clear();
        }

        private void ExportData()
        {
            if (_graphAnalyser.TotalNodes() > 1000 && _graphAnalyser.TotalEdges() > 1000)
            {
                var gexfExporter = new GexfGraphExporter();
                var data = gexfExporter.Export(_nodes);
            }
        }
    }
}
