using System;
using System.Data;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.WebGraph.Adapters.Memory
{
    public class MemoryWebGraphAdapter : BaseWebGraph
    {
        // Data holders for simulation of DB tables
        private readonly Dictionary<Guid, Graph> _graphTable = new();
        private readonly Dictionary<Guid, Dictionary<string, Node>> _nodeTable = new();

        public MemoryWebGraphAdapter(ILogger logger, GraphingSettings graphingSettings) 
            : base(logger, graphingSettings) { }

        public async override Task<Node?> GetNodeAsync(Guid graphId, string url)
        {
            if (_nodeTable.TryGetValue(graphId, out var nodes))
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

            if (!_nodeTable.TryGetValue(node.GraphId, out var nodes))
            {
                nodes = new Dictionary<string, Node>();
                _nodeTable[node.GraphId] = nodes;
            }

            node.ModifiedAt = DateTimeOffset.UtcNow;
            nodes[node.Url] = node;

            return await Task.FromResult(node);
        }

        protected override Task<bool> AddOutgoingLinkAsync(Guid graphId, Node fromNode, Node toNode)
        {
            if (fromNode.OutgoingLinks.Contains(toNode))
                return Task.FromResult(false);

            fromNode.OutgoingLinks.Add(toNode);
            return Task.FromResult(true);
        }

        protected override Task<bool> AddIncomingLinkAsync(Guid graphId, Node toNode, Node fromNode)
        {
            if (toNode.IncomingLinks.Contains(fromNode))
                return Task.FromResult(false);

            toNode.IncomingLinks.Add(fromNode);
            return Task.FromResult(true);
        }

        protected override Task ClearOutgoingLinksAsync(Guid graphId, Node node)
        {
            foreach (var target in node.OutgoingLinks.ToList()) // copy to avoid modifying while iterating
            {
                target.IncomingLinks.Remove(node);
            }

            node.OutgoingLinks.Clear();
            return Task.CompletedTask;
        }

        protected override Task<int> GetPopularityScoreAsync(Guid graphId, Node node)
        {
            // Simple metric: sum of incoming + outgoing links
            var score = node.IncomingLinks.Count + node.OutgoingLinks.Count;
            return Task.FromResult(score);
        }

        public async override Task CleanupOrphanedNodesAsync(Guid graphId)
        {
            if (!_nodeTable.TryGetValue(graphId, out var nodes))
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


        public override Task<IEnumerable<Node>> GetInitialGraphNodes(Guid graphId, int topN)
        {
            if (!_nodeTable.TryGetValue(graphId, out var nodes))
                return Task.FromResult(Enumerable.Empty<Node>());

            var popularNodes = nodes.Values
                .Where(n => n.State == NodeState.Populated)  // only populated nodes
                .OrderByDescending(n => n.PopularityScore)   // sort by popularity
                .ThenByDescending(n => n.ModifiedAt)         // then by latest modified
                .Take(topN)                                  // take top N
                .ToList();

            return Task.FromResult<IEnumerable<Node>>(popularNodes);
        }

        public override async Task<long> TotalPopulatedNodesAsync(Guid graphId)
        {
            if (_nodeTable.TryGetValue(graphId, out var nodes))
            {
                int count = nodes.Values.Count(n => n.State == NodeState.Populated);
                return await Task.FromResult(count);
            }

            return await Task.FromResult(0);
        }




        public override async Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null)
        {
            if (!_nodeTable.TryGetValue(graphId, out var nodes) || !nodes.TryGetValue(startUrl, out var startNode))
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


        public override async Task<Graph?> GetGraphAsync(Guid graphId)
        {
            _graphTable.TryGetValue(graphId, out var graph);
            return graph;
        }

        public override async Task<Graph?> CreateGraphAsync(GraphOptions options)
        {
            var graph = new Graph
            {
                Id = Guid.NewGuid(),
                Name = options.Name,
                Description = options.Description,
                Url = options.Url?.AbsoluteUri ?? string.Empty,
                MaxDepth = options.MaxDepth,
                MaxLinks = options.MaxLinks,
                ExcludeExternalLinks = options.ExcludeExternalLinks,
                ExcludeQueryStrings = options.ExcludeQueryStrings,
                UrlMatchRegex = options.UrlMatchRegex,
                TitleElementXPath = options.TitleElementXPath,
                ContentElementXPath = options.ContentElementXPath,
                SummaryElementXPath = options.SummaryElementXPath,
                ImageElementXPath = options.ImageElementXPath,
                RelatedLinksElementXPath = options.RelatedLinksElementXPath,
                CreatedAt = DateTimeOffset.UtcNow,
                UserAgent = options.UserAgent,
                UserAccepts = options.UserAccepts
            };

            // Save into Graph “table”
            _graphTable[graph.Id] = graph;

            // Initialise node storage for this graph
            _nodeTable[graph.Id] = new Dictionary<string, Node>();

            return graph;
        }

        public override async Task<Graph?> UpdateGraphAsync(Graph graph)
        {
            if (!_graphTable.ContainsKey(graph.Id))
                throw new KeyNotFoundException($"Graph {graph.Id} not found.");

            _graphTable[graph.Id] = graph;
            return graph;
        }

        public override Task<Graph?> DeleteGraphAsync(Guid graphId)
        {
            // Try get the graph before removal
            if (!_graphTable.TryGetValue(graphId, out var graph))
            {
                return Task.FromResult<Graph?>(null);
            }

            // Remove graph metadata
            _graphTable.Remove(graphId);

            // Remove associated nodes (cascade delete)
            _nodeTable.Remove(graphId);

            return Task.FromResult<Graph?>(graph);
        }

        public override async Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;

            var items = _graphTable.Values
                .OrderBy(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedResult<Graph>(
                items,
                _graphTable.Count,
                page,
                pageSize);

            return result;
        }


        public override async Task<string> DumpGraphContentsAsync(Guid graphId)
        {
            var sb = new System.Text.StringBuilder();

            try
            {
                // Get initial node (seed for traversal)
                var initialNodes = await GetInitialGraphNodes(graphId, 1);
                var startNode = initialNodes.FirstOrDefault();
                if (startNode == null)
                {
                    sb.AppendLine($"Graph {graphId} — No nodes found.");
                    return sb.ToString();
                }

                // Hydrate neighborhood
                var nodes = await GetNodeNeighborhoodAsync(graphId, startNode.Url, maxDepth: 3, maxNodes: null);
                var nodeList = nodes.ToList();

                sb.AppendLine($"Graph {graphId} — Total Nodes: {nodeList.Count}");
                sb.AppendLine($"Neighborhood start: {startNode.Url}");

                foreach (var node in nodeList.OrderBy(n => n.Url))
                {
                    sb.AppendLine($"  Node: {node.Url}");
                    sb.AppendLine($"    State: {node.State}");
                    sb.AppendLine($"    Title: {node.Title}");
                    sb.AppendLine($"    Popularity: {node.PopularityScore}");
                    sb.AppendLine($"    Incoming: {node.IncomingLinks.Count} | Outgoing: {node.OutgoingLinks.Count}");

                    if (node.OutgoingLinks.Any())
                    {
                        sb.AppendLine("    Outgoing Links:");
                        foreach (var outNode in node.OutgoingLinks.OrderBy(n => n.Url))
                            sb.AppendLine($"      -> {outNode.Url} [{outNode.State}]");
                    }

                    if (node.IncomingLinks.Any())
                    {
                        sb.AppendLine("    Incoming Links:");
                        foreach (var inNode in node.IncomingLinks.OrderBy(n => n.Url))
                            sb.AppendLine($"      <- {inNode.Url} [{inNode.State}]");
                    }
                }

                sb.AppendLine(new string('-', 50));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dump graph contents for GraphId {GraphId}", graphId);
                sb.AppendLine($"Error dumping graph {graphId}: {ex.Message}");
            }

            return sb.ToString();
        }


    }
}
