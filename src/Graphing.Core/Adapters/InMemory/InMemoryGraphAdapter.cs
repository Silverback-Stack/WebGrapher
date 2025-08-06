//using System;
//using System.Collections.Concurrent;
//using Events.Core.Bus;
//using Graphing.Core.Exporters;
//using Graphing.Core.Models;
//using Microsoft.Extensions.Logging;

//namespace Graphing.Core.Adapters.InMemory
//{
//    public class InMemoryGraphAdapter : BaseGraph
//    {
//        private readonly IGraphAnalyser _graphAnalyser;
//        private readonly ConcurrentDictionary<string, Node> _nodes;

//        /// <summary>
//        /// In-memory graph adapter for local development, 
//        /// can be swapped out with a graph db adapter such as CosmoGraph.
//        /// </summary>
//        public InMemoryGraphAdapter(ILogger logger, IEventBus eventBus) : base(logger, eventBus)
//        {
//            _nodes = new ConcurrentDictionary<string, Node>();
//            _graphAnalyser = new InMemoryGraphAnalyserAdapter(_nodes);
//        }

//        public IReadOnlyDictionary<string, Node> GetNodes() => _nodes;

//        public override IGraphAnalyser GraphAnalyser => _graphAnalyser;

//        private void ExportData()
//        {
//            if (_graphAnalyser.TotalNodes() > 1000 && _graphAnalyser.TotalEdges() > 1000)
//            {
//                var gexfExporter = new GexfGraphExporter();
//                var data = gexfExporter.Export(_nodes);
//            }
//        }

//        public override Task<Node?> GetNodeAsync(string id)
//        {
//            ExportData();
//            _nodes.TryGetValue(id, out var node);
//            return Task.FromResult<Node?>(node);
//        }

//        public override Task<Node?> SetNodeAsync(Node node)
//        {
//            _nodes[node.Id] = node;
//            return Task.FromResult<Node?>(node);
//        }

//        public override Task DeleteNodeAsync(string id)
//        {
//            _nodes.TryRemove(id, out _);
//            return Task.CompletedTask;
//        }
//        public override void Dispose()
//        {
//            _logger.LogDebug($"Disposing: {typeof(InMemoryGraphAdapter).Name}, memory cleared.");
//            _nodes.Clear();
//        }
//    }
//}
