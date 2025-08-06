using System;
using System.Data;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.Version2
{
    public class InMemoryWebGraphAdapter : BaseWebGraph
    {
        private readonly Dictionary<int, Dictionary<string, WebGraphNode>> _graphs = new();

        public InMemoryWebGraphAdapter(
            ILogger logger, IEventBus eventBus) : base(logger, eventBus) { }

        public async override Task<WebGraphNode?> GetNodeAsync(int graphId, string url)
        {
            if (_graphs.TryGetValue(graphId, out var nodes))
            {
                nodes.TryGetValue(url, out var node);
                return await Task.FromResult<WebGraphNode?>(node);
            }

            return await Task.FromResult<WebGraphNode?>(null);
        }

        protected async override Task<WebGraphNode> SetNodeAsync(WebGraphNode node)
        {
            var storedNode = await GetNodeAsync(node.GraphId, node.Url);
            if (storedNode != null &&
                storedNode.ModifiedAt > node.ModifiedAt)
            {
                _logger.LogDebug($"SetNodeAsync skipped for URL {node.Url} in GraphId: {node.GraphId} due to stale data. Incoming ModifiedAt: {node.ModifiedAt}, Stored ModifiedAt: {storedNode.ModifiedAt}");
                // Node has been modified by another process since it was read
                // FUTURE FEATURE: Decide on strategy:
                //   "skip" (the current behavior)
                //   "force" (overwrite anyway)
                //   "merge" (not trivial, only if merging fields is feasible)
                return storedNode;
            }

            if (!_graphs.TryGetValue(node.GraphId, out var nodes))
            {
                nodes = new Dictionary<string, WebGraphNode>();
                _graphs[node.GraphId] = nodes;
            }

            node.ModifiedAt = DateTimeOffset.UtcNow;
            nodes[node.Url] = node;
            return await Task.FromResult(node);
        }

        public async override Task CleanupOrphanedNodesAsync(int graphId)
        {
            if (!_graphs.TryGetValue(graphId, out var nodes))
            {
                _logger.LogDebug($"No nodes found to cleanup in the graph: {graphId}");
                return;
            }

            var referenced = new HashSet<WebGraphNode>();

            // Find all nodes that are referenced (have incoming edges)
            foreach (var node in nodes.Values)
            {
                foreach (var target in node.OutgoingLinks)
                {
                    if (target.GraphId == graphId)
                    {
                        referenced.Add(target);
                    }
                }
            }

            // Find orphan nodes: Redirected or Dummy nodes not referenced by anyone
            var orphans = nodes.Values
                .Where(n => (n.State == NodeState.Redirected || n.State == NodeState.Dummy)
                            && !referenced.Contains(n))
                .ToList();

            // Remove orphan nodes
            foreach (var orphan in orphans)
            {
                nodes.Remove(orphan.Url);
                Console.WriteLine($"[Cleanup] Removed orphan node: {orphan.Url} [{orphan.State}]");
            }

            await Task.CompletedTask;
        }

        public override async Task<int> TotalPopulatedNodesAsync(int graphId)
        {
            if (_graphs.TryGetValue(graphId, out var nodes))
            {
                int count = nodes.Values.Count(n => n.State == NodeState.Populated);
                return await Task.FromResult(count);
            }

            return await Task.FromResult(0);
        }

    }
}
