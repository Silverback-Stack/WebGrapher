using System;
using System.Data;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.WebGraph.Adapters.InMemory
{
    public class InMemoryWebGraphAdapter : BaseWebGraph
    {
        private readonly Dictionary<int, Dictionary<string, Node>> _graphs = new();

        public InMemoryWebGraphAdapter(ILogger logger) : base(logger) { }

        public async override Task<Node?> GetNodeAsync(int graphId, string url)
        {
            if (_graphs.TryGetValue(graphId, out var nodes))
            {
                nodes.TryGetValue(url, out var node);
                return await Task.FromResult(node);
            }

            return await Task.FromResult<Node?>(null);
        }

        public async override Task<Node> SetNodeAsync(Node node)
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
                nodes = new Dictionary<string, Node>();
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

            var referenced = new HashSet<Node>();

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
            _logger.LogWarning(DumpGraphContents());

            if (_graphs.TryGetValue(graphId, out var nodes))
            {
                int count = nodes.Values.Count(n => n.State == NodeState.Populated);
                return await Task.FromResult(count);
            }

            return await Task.FromResult(0);
        }

        public async Task<IEnumerable<Node>?> GetMostPopularNodesAsync(int graphId, int limit)
        {
            if (!_graphs.TryGetValue(graphId, out var nodes) || nodes.Count == 0)
            {
                _logger.LogDebug($"Graph {graphId} is empty. Cannot determine most popular node.");
                return await Task.FromResult<IEnumerable<Node>?>(null);
            }

            var mostPopular = nodes.Values
                .OrderByDescending(n => n.IncomingLinkCount)
                .ThenByDescending(n => n.CreatedAt) // Tie-breaker: newest first
                .Take(limit);

            return await Task.FromResult(mostPopular);
        }

        public async Task<IEnumerable<Node>> TraverseGraphAsync(int graphId, string startUrl, int maxDepth, int? maxNodes = null)
        {
            if (!_graphs.TryGetValue(graphId, out var nodes) || !nodes.TryGetValue(startUrl, out var startNode))
            {
                _logger.LogDebug($"Graph {graphId} or start node {startUrl} not found.");
                return Enumerable.Empty<Node>();
            }

            var visited = new HashSet<string>();
            var result = new List<Node>();
            var queue = new Queue<(Node node, int depth)>();

            queue.Enqueue((startNode, 0));
            visited.Add(startNode.Url);

            while (queue.Count > 0)
            {
                var (currentNode, currentDepth) = queue.Dequeue();
                result.Add(currentNode);

                if (maxNodes.HasValue && result.Count >= maxNodes.Value)
                {
                    break;
                }

                if (currentDepth >= maxDepth)
                {
                    continue;
                }

                foreach (var neighbor in currentNode.OutgoingLinks)
                {
                    if (visited.Add(neighbor.Url))
                    {
                        queue.Enqueue((neighbor, currentDepth + 1));
                    }
                }
            }

            return await Task.FromResult(result);
        }


        public string DumpGraphContents()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var (graphId, nodes) in _graphs)
            {
                sb.AppendLine($"Graph {graphId} — Total Nodes: {nodes.Count}");
                foreach (var node in nodes.Values.OrderBy(n => n.Url))
                {
                    sb.AppendLine($"  Node: {node.Url}");
                    sb.AppendLine($"    State: {node.State}");
                    sb.AppendLine($"    Title: {node.Title}");
                    sb.AppendLine($"    Incoming: {node.IncomingLinkCount} | Outgoing: {node.OutgoingLinkCount}");

                    if (node.OutgoingLinks.Any())
                    {
                        sb.AppendLine("    Outgoing Links:");
                        foreach (var outNode in node.OutgoingLinks)
                        {
                            sb.AppendLine($"      -> {outNode.Url} [{outNode.State}]");
                        }
                    }

                    if (node.IncomingLinks.Any())
                    {
                        sb.AppendLine("    Incoming Links:");
                        foreach (var inNode in node.IncomingLinks)
                        {
                            sb.AppendLine($"      <- {inNode.Url} [{inNode.State}]");
                        }
                    }
                }
                sb.AppendLine(new string('-', 50));
            }

            return sb.ToString();
        }

    }
}
