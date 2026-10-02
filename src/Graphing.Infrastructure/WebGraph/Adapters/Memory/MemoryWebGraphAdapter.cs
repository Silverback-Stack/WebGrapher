using System;
using System.Data;
using Graphing.Core;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Models;
using Microsoft.Extensions.Logging;

namespace Graphing.Infrastructure.WebGraph.Adapters.Memory
{

    /// <summary>
    /// In-memory WebGraph adapter used to simulate database storage.
    /// Data is volatile and is not persisted.
    /// </summary>
    public class MemoryWebGraphAdapter : BaseWebGraph
    {
        // Data store for Graph objects, keyed by GraphId.
        private readonly Dictionary<Guid, Graph> _graphTable = new();

        // Data store for Node objects.
        // The outer dictionary is keyed by GraphId.
        // The inner dictionary is keyed by Node URL.
        private readonly Dictionary<Guid, Dictionary<string, Node>> _nodeTable = new();

        public MemoryWebGraphAdapter(
            ILogger logger, 
            GraphingSettings graphingSettings) 
            : base(logger, graphingSettings) { }


        // #############
        // Graph storage
        // #############

        /// <summary>
        /// Creates a new Graph and initialises its Node storage.
        /// </summary>
        public override Task<Graph> CreateGraphAsync(
            Guid graphId, 
            string userId, 
            GraphOptions options)
        {
            var graph = new Graph
            {
                Id = graphId,
                UserId = userId,
                Name = options.Name,
                Description = options.Description,
                Url = options.Url?.AbsoluteUri ?? string.Empty,
                MaxDepth = options.MaxDepth,
                MaxLinks = options.MaxLinks,
                ExcludeExternalLinks = options.ExcludeExternalLinks,
                ExcludeQueryStrings = options.ExcludeQueryStrings,
                ConsolidateQueryStrings = options.ConsolidateQueryStrings,
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

            // Store the Graph using its Id as the lookup key.
            _graphTable[graph.Id] = graph;

            // Create an empty Node store for this Graph.
            _nodeTable[graph.Id] = new Dictionary<string, Node>();

            return Task.FromResult(graph);
        }


        /// <summary>
        /// Retrieves a Graph owned by the specified user.
        /// </summary>
        public override Task<Graph?> GetGraphAsync(
            Guid graphId, 
            string userId)
        {
            _graphTable.TryGetValue(graphId, out var graph);

            if (graph == null)
                return Task.FromResult<Graph?>(null);

            // Treat Graphs owned by another user as not found.
            if (graph.UserId != userId)
                return Task.FromResult<Graph?>(null);

            return Task.FromResult<Graph?>(graph);
        }


        /// <summary>
        /// Updates an existing Graph owned by the specified user.
        /// </summary>
        public override Task<Graph> UpdateGraphAsync(
            Graph graph, 
            string userId)
        {
            var existingGraph = GetGraphAsync(graph.Id, userId).Result;

            if (existingGraph == null)
                throw new KeyNotFoundException($"Graph {graph.Id} not found.");

            _graphTable[graph.Id] = graph;

            return Task.FromResult(graph);
        }


        /// <summary>
        /// Deletes a Graph and its associated Nodes.
        /// </summary>
        public override Task<Graph?> DeleteGraphAsync(
            Guid graphId, 
            string userId)
        {
            var existingGraph = GetGraphAsync(graphId, userId).Result;

            if (existingGraph == null)
                return Task.FromResult<Graph?>(null);

            // Remove all Nodes associated with the Graph.
            _nodeTable.Remove(graphId);

            // Remove the Graph.
            _graphTable.Remove(graphId);

            // Return the deleted Graph to the caller.
            return Task.FromResult<Graph?>(existingGraph);
        }


        /// <summary>
        /// Returns a paged list of Graphs owned by the specified user.
        /// </summary>
        public override Task<PagedResult<Graph>> ListGraphsAsync(
            int page, 
            int pageSize, 
            string userId)
        {
            // Ensure paging values are valid.
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;

            // Restrict results to Graphs owned by the specified user.
            var filtered = _graphTable.Values
                .Where(g => g.UserId == userId);

            // Order the Graphs and select the requested page.
            var items = filtered
                .OrderBy(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Return the page together with the total number of matching Graphs.
            var result = new PagedResult<Graph>(
                items,
                filtered.Count(),
                page,
                pageSize);

            return Task.FromResult(result);
        }



        // #############
        // Node Storage
        // #############

        /// <summary>
        /// Retrieves a Node from the WebGraph.
        /// </summary>
        public async override Task<Node?> GetNodeAsync(
            Guid graphId, 
            string url)
        {
            // Find the Node store for the requested Graph.
            if (_nodeTable.TryGetValue(graphId, out var nodes))
            {
                // Find the Node using its URL as the lookup key.
                nodes.TryGetValue(url, out var node);

                return await Task.FromResult(node);
            }

            return await Task.FromResult<Node?>(null);
        }


        /// <summary>
        /// Stores a Node in the WebGraph.
        /// </summary>
        public async override Task<Node> SetNodeAsync(Node node)
        {
            var storedNode = await GetNodeAsync(node.GraphId, node.Url);

            // Prevent an older version of the Node from overwriting newer data.
            if (storedNode != null &&
                storedNode.ModifiedAt > node.ModifiedAt)
            {
                _logger.LogDebug(
                    "SetNodeAsync skipped for URL {Url} in GraphId: {GraphId} due to stale data. Incoming ModifiedAt: {NodeModifiedAt}, Stored ModifiedAt: {StoredNodeModifiedAt}",
                    node.Url,
                    node.GraphId,
                    node.ModifiedAt,
                    storedNode.ModifiedAt);

                return storedNode;
            }

            // Get a reference to this Graph's Nodes collection.
            var nodes = _nodeTable[node.GraphId];

            // Store the latest version of the Node in the collection using its URL as the lookup key.
            node.ModifiedAt = DateTimeOffset.UtcNow;
            nodes[node.Url] = node;

            return await Task.FromResult(node);
        }


        /// <summary>
        /// Adds an outgoing relationship from one Node to another.
        /// </summary>
        protected override Task<bool> AddOutgoingRelationshipAsync(
            Guid graphId, 
            Node fromNode, 
            Node toNode)
        {
            // Do not add the relationship if it already exists.
            if (fromNode.OutgoingNodes.Contains(toNode))
                return Task.FromResult(false);

            fromNode.OutgoingNodes.Add(toNode);

            return Task.FromResult(true);
        }


        /// <summary>
        /// Adds the corresponding incoming relationship to the target Node.
        /// </summary>
        protected override Task<bool> AddIncomingRelationshipAsync(
            Guid graphId, 
            Node toNode, 
            Node fromNode)
        {
            // Do not add the relationship if it already exists.
            if (toNode.IncomingNodes.Contains(fromNode))
                return Task.FromResult(false);

            toNode.IncomingNodes.Add(fromNode);

            return Task.FromResult(true);
        }


        /// <summary>
        /// Removes all outgoing relationships from a Node.
        /// </summary>
        protected override Task ClearOutgoingRelationshipsAsync(
            Guid graphId, 
            Node node)
        {
            // Copy the outgoing collection so relationships can be removed safely while iterating.
            foreach (var target in node.OutgoingNodes.ToList())
            {
                // Remove the corresponding incoming relationship from the target Node.
                target.IncomingNodes.Remove(node);
            }

            // Remove all outgoing relationships from the source Node.
            node.OutgoingNodes.Clear();

            return Task.CompletedTask;
        }


        /// <summary>
        /// Calculates the popularity score for a Node.
        /// </summary>
        protected override Task<int> GetPopularityScoreAsync(
            Guid graphId, 
            Node node)
        {
            // Use the total number of incoming and outgoing relationships as the popularity score.
            var score = node.IncomingNodes.Count + node.OutgoingNodes.Count;

            return Task.FromResult(score);
        }



        // #############
        // Graph Queries
        // #############

        /// <summary>
        /// Returns the first populated Nodes to use as starting points for graph traversal.
        /// </summary>
        public override Task<IEnumerable<Node>> GetInitialGraphNodes(
            Guid graphId, 
            int topN)
        {
            // Get this Graph's Nodes collection,
            // or return an empty result if it does not exist.
            if (!_nodeTable.TryGetValue(graphId, out var nodes))
                return Task.FromResult(Enumerable.Empty<Node>());

            // Select populated Nodes only, ordered by when they were first created.
            var initialNodes = nodes.Values
                .Where(n => n.State == NodeState.Populated)
                .OrderBy(n => n.CreatedAt)
                .Take(topN)
                .ToList();

            return Task.FromResult<IEnumerable<Node>>(initialNodes);
        }


        /// <summary>
        /// Traverses the WebGraph from a starting Node using breadth-first search (BFS).
        /// Nodes are visited level by level through outgoing relationships, while
        /// tracking visited URLs to prevent cycles and respecting depth and Node limits.
        /// </summary>
        public override async Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null)
        {
            // Get this Graph's Nodes collection and locate the starting Node.
            if (!_nodeTable.TryGetValue(graphId, out var nodes) || 
                !nodes.TryGetValue(startUrl, out var startNode))
            {
                _logger.LogDebug(
                    "Graph {GraphId} or start node {StartUrl} not found.",
                    graphId, 
                    startUrl);

                return Enumerable.Empty<Node>();
            }

            // Track visited Nodes to prevent the traversal from revisiting the same URL.
            var visited = new HashSet<string>();

            // Store the Nodes discovered during the traversal.
            var result = new List<Node>();

            // Use a queue to traverse the graph breadth-first while tracking depth.
            var queue = new Queue<(Node node, int depth)>();

            // Start the traversal from the requested Node at depth zero.
            queue.Enqueue((startNode, 0));
            visited.Add(startNode.Url);

            while (queue.Count > 0)
            {
                // Take the next Node from the queue together with its traversal depth.
                var (currentNode, currentDepth) = queue.Dequeue();

                // Add the current Node to the traversal results.
                result.Add(currentNode);

                // Stop when the requested Node limit has been reached.
                if (maxNodes.HasValue && 
                    result.Count >= maxNodes.Value)
                {
                    break;
                }

                // Do not traverse beyond the requested depth.
                if (currentDepth >= maxDepth)
                {
                    continue;
                }

                // Add unvisited outgoing Nodes to the queue for the next depth.
                foreach (var neighbor in currentNode.OutgoingNodes)
                {
                    if (visited.Add(neighbor.Url))
                    {
                        queue.Enqueue(
                            (neighbor, currentDepth + 1));
                    }
                }
            }

            return await Task.FromResult(result);
        }


        /// <summary>
        /// Returns the total number of populated Nodes in the WebGraph.
        /// </summary>
        public override async Task<long> TotalPopulatedNodesAsync(Guid graphId)
        {
            // Get this Graph's Nodes collection, or return zero if it does not exist.
            if (_nodeTable.TryGetValue(graphId, out var nodes))
            {
                var count = nodes.Values.Count(
                    n => n.State == NodeState.Populated);

                return await Task.FromResult(count);
            }

            return await Task.FromResult(0);
        }






        // #################
        // Graph Maintenance
        // #################

        /// <summary>
        /// Removes unreferenced Dummy and Redirected Nodes from the WebGraph.
        /// </summary>
        public async override Task CleanupOrphanedNodesAsync(Guid graphId)
        {
            // Get this Graph's Nodes collection, or return if it does not exist.
            if (!_nodeTable.TryGetValue(graphId, out var nodes))
            {
                _logger.LogDebug(
                    "No nodes found to cleanup in the graph: {GraphId}", 
                    graphId);

                return;
            }

            // Track Nodes that are still referenced by an incoming relationship.
            var referenced = new HashSet<Node>();

            foreach (var node in nodes.Values)
            {
                foreach (var target in node.OutgoingNodes)
                {
                    if (target.GraphId == graphId)
                    {
                        referenced.Add(target);
                    }
                }
            }

            // Find Dummy or Redirected Nodes that are no longer referenced.
            var orphans = nodes.Values
                .Where(n => 
                    (n.State == NodeState.Redirected ||
                    n.State == NodeState.Dummy) && 
                    !referenced.Contains(n))
                .ToList();

            // Remove the orphaned Nodes from the Graph.
            foreach (var orphan in orphans)
            {
                nodes.Remove(orphan.Url);

                Console.WriteLine(
                    $"[Cleanup] Removed orphan node: {orphan.Url} [{orphan.State}]");
            }

            await Task.CompletedTask;
        }


    }
}
