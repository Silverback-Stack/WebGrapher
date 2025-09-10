﻿using System;
using System.Net.WebSockets;
using Graphing.Core.WebGraph.Models;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Logging;
using Graph = Graphing.Core.WebGraph.Models.Graph;

namespace Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin
{
    internal class AzureCosmosGremlinWebGraphAdapter : BaseWebGraph
    {
        private readonly GremlinClient _gremlinClient;
        private readonly IGremlinQueryProvider _gremlinQueryProvider;

        public AzureCosmosGremlinWebGraphAdapter(ILogger logger, GraphingSettings graphingSettings)
            : base(logger, graphingSettings)
        {
            try
            {
                var gremlinServer = new GremlinServer(
                    graphingSettings.WebGraph.AzureCosmosGremlin.Hostname,
                    graphingSettings.WebGraph.AzureCosmosGremlin.Port,
                    graphingSettings.WebGraph.AzureCosmosGremlin.EnableSsl,
                    username: $"/dbs/{graphingSettings.WebGraph.AzureCosmosGremlin.Database}/colls/{graphingSettings.WebGraph.AzureCosmosGremlin.Graph}",
                    password: graphingSettings.WebGraph.AzureCosmosGremlin.PrimaryKey
                );

                _gremlinClient = new GremlinClient(
                    gremlinServer,
                    new GraphSON2MessageSerializer(new CustomGraphSON2Reader())
                );
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Invalid argument while creating GremlinClient. Check Hostname, Database, Graph, or PrimaryKey.");
                throw;
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogError(wsEx, "Failed to connect to Gremlin endpoint. Check network connectivity and SSL settings.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating GremlinClient.");
                throw;
            }

            _gremlinQueryProvider = new GremlinQueryProvider(logger, _gremlinClient);
        }


        //Graph Operations

        public override async Task<Graph?> GetGraphAsync(Guid graphId)
        {
            var vertex = await _gremlinQueryProvider.GetGraphVertexAsync(graphId);
            if (vertex == null) return null;

            return GremlinQueryHelper.HydrateGraphFromVertex(vertex);
        }

        public override async Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 8;

            int start = (page - 1) * pageSize;
            int end = start + pageSize;

            var graphs = new List<Graph>();

            var vertices = await _gremlinQueryProvider.ListGraphVerticesAsync(start, end);

            foreach (var vertex in vertices)
            {
                if (vertex is not IDictionary<string, object>)
                    continue;

                var graph = GremlinQueryHelper.HydrateGraphFromVertex(vertex);
                graphs.Add(graph);
            }

            var totalGraphs = await _gremlinQueryProvider.CountGraphVerticesAsync();

            return new PagedResult<Graph>(
                graphs,
                totalGraphs,
                page,
                pageSize
            );
        }

        public override async Task<Graph> CreateGraphAsync(GraphOptions options)
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

            var vertex = await _gremlinQueryProvider.CreateGraphVertexAsync(graph);
            if (vertex == null) throw new InvalidOperationException("Unable to create graph.");

            return GremlinQueryHelper.HydrateGraphFromVertex(vertex);
        }

        public override async Task<Graph?> UpdateGraphAsync(Graph graph)
        {
            await _gremlinQueryProvider.UpdateGraphVertexAsync(graph);

            return graph;
        }

        public override async Task<Graph?> DeleteGraphAsync(Guid graphId)
        {
            // Fetch the graph first so we can return it after deletion
            var graph = await GetGraphAsync(graphId);
            if (graph == null) return null;

            await _gremlinQueryProvider.DeleteGraphVertexAsync(graphId);

            return graph;
        }



        // Node Operations

        public override async Task<Node?> GetNodeAsync(Guid graphId, string url)
        {
            var vertex = await _gremlinQueryProvider.GetNodeVertexAsync(graphId, url);
            if (vertex == null) return null;

            var node = GremlinQueryHelper.HydrateNodeFromVertex(vertex, graphId);

            var nodeEdges = await _gremlinQueryProvider.GetNodeVerticesEdgesAsync(graphId, new[] { url });

            //Populate links
            foreach (var (source, target, isOutgoing) in nodeEdges)
            {
                if (isOutgoing)
                    node.OutgoingLinks.Add(GremlinQueryHelper.HydrateNodeFromVertex(target, graphId));
                else
                    node.IncomingLinks.Add(GremlinQueryHelper.HydrateNodeFromVertex(source, graphId));
            }

            return node;
        }

        public override async Task<Node> SetNodeAsync(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var storedNode = await GetNodeAsync(node.GraphId, node.Url);

            // Check for stale updates
            if (storedNode != null && storedNode.ModifiedAt > node.ModifiedAt)
            {
                _logger.LogDebug(
                    "SetNodeAsync skipped for {url} due to stale data. Incoming: {incoming}, Stored: {stored}",
                    node.Url, node.ModifiedAt, storedNode.ModifiedAt
                );
                return storedNode;
            }

            await _gremlinQueryProvider.UpsertNodeVertexAsync(node);

            return node;
        }

        protected async override Task<bool> AddOutgoingLinkAsync(Guid graphId, Node fromNode, Node toNode)
        {
            await _gremlinQueryProvider.AddNodeVertexEdgeAsync(fromNode, toNode, graphId);
            return true;
        }

        protected async override Task<bool> AddIncomingLinkAsync(Guid graphId, Node toNode, Node fromNode)
        {
            // Same as outgoing, just reversed
            return await AddOutgoingLinkAsync(graphId, fromNode, toNode);
        }

        protected async override Task ClearOutgoingLinksAsync(Guid graphId, Node node)
        {
            await _gremlinQueryProvider.RemoveNodeVertexEdgesAsync(graphId, node);
        }

        public override async Task CleanupOrphanedNodesAsync(Guid graphId)
        {
            await _gremlinQueryProvider.RemoveOrphanedNodeVerticesAsync(graphId);
        }

        protected async override Task<int> GetPopularityScoreAsync(Guid graphId, Node node)
        {
            return await _gremlinQueryProvider.CountNodeVertexEdgesAsync(graphId, node);
        }

        public override async Task<IEnumerable<Node>> GetInitialGraphNodes(Guid graphId, int topN)
        {
            var results = await _gremlinQueryProvider.GetNodeVerticesByCreationDateAscAsync(graphId, topN);

            // Convert dynamic results into Node objects
            return results.Select<dynamic, Node>(v =>
                GremlinQueryHelper.HydrateNodeFromVertex(v, graphId));
        }

        public override async Task<long> TotalPopulatedNodesAsync(Guid graphId)
        {
            return await _gremlinQueryProvider.CountNodeVerticesPopulatedAsync(graphId);
        }

        public override async Task<IEnumerable<Node>> GetNodeNeighborhoodAsync(
            Guid graphId, string startUrl, int maxDepth, int? maxNodes = null)
        {

            var vertex = await _gremlinQueryProvider.GetNodeVertexAsync(graphId, startUrl);
            if (vertex == null) return Enumerable.Empty<Node>();

            var vertexId = vertex["id"].ToString();

            var results = await _gremlinQueryProvider.GetNodeVertexSubgraphAsync(graphId, vertexId, maxDepth, maxNodes);
            if (results.Count == 0) return Enumerable.Empty<Node>();

            var vertexArray = results; // each item is a vertex
            var nodeMap = new Dictionary<string, Node>();

            // Hydrate nodes first
            foreach (var vertexObj in vertexArray)
            {
                if (vertexObj is IDictionary<string, object> vertexDict)
                {
                    var node = GremlinQueryHelper.HydrateNodeFromVertex(vertexDict, graphId);
                    nodeMap[node.Id.ToString()] = node;
                }
            }

            // Hydrate edges
            foreach (var vertexObj in vertexArray)
            {
                if (!(vertexObj is IDictionary<string, object> vertexDict)) continue;

                if (vertexDict.TryGetValue("outE", out var outEDictObj) && outEDictObj is IDictionary<string, object> outEDict)
                {
                    foreach (var kvp in outEDict) // kvp.Key = edge label, kvp.Value = list of edges
                    {
                        if (kvp.Value is IEnumerable<object> edgeList)
                        {
                            foreach (var edgeObj in edgeList)
                            {
                                if (!(edgeObj is IDictionary<string, object> edgeDict)) continue;

                                var outV = edgeDict["outV"].ToString();
                                var inV = edgeDict["inV"].ToString();

                                // Only link if both nodes exist
                                if (nodeMap.TryGetValue(outV, out Node fromNode) &&
                                    nodeMap.TryGetValue(inV, out Node toNode))
                                {
                                    fromNode.OutgoingLinks.Add(toNode);
                                    toNode.IncomingLinks.Add(fromNode);
                                }
                            }
                        }
                    }
                }
            }

            return nodeMap.Values;

        }











        public async Task<string> DumpGraphContentsAsync(Guid graphId)
        {
            var sb = new System.Text.StringBuilder();

            try
            {
                // Fetch all Node vertices for the graph
                const string allNodesQuery = "g.V().has('graphId', gId).hasLabel('node')";
                var parameters = new Dictionary<string, object>
                {
                    ["gId"] = graphId.ToString()
                };

                var results = await _gremlinClient.SubmitAsync<dynamic>(allNodesQuery, parameters);

                var nodes = new Dictionary<Guid, Node>();

                foreach (var vertex in results)
                {
                    var node = GremlinQueryHelper.HydrateNodeFromVertex(vertex, graphId);
                    nodes[node.Id] = node;
                }

                // Hydrate edges (outgoing)
                foreach (var node in nodes.Values)
                {
                    const string outgoingQuery = @"
                g.V(vId)
                 .outE('linksTo')
                 .inV()
                 .has('graphId', gId)
                 .hasLabel('node')
            ";
                    var outgoingResults = await _gremlinClient.SubmitAsync<dynamic>(outgoingQuery, new Dictionary<string, object>
                    {
                        ["vId"] = node.Id.ToString(),
                        ["gId"] = graphId.ToString()
                    });

                    foreach (var targetVertex in outgoingResults)
                    {
                        var targetNodeId = Guid.Parse(targetVertex["id"].ToString());
                        Node targetNode;
                        if (nodes.TryGetValue(targetNodeId, out targetNode))
                            node.OutgoingLinks.Add(targetNode);
                    }
                }

                // Hydrate edges (incoming)
                foreach (var node in nodes.Values)
                {
                    const string incomingQuery = @"
                g.V(vId)
                 .inE('linksTo')
                 .outV()
                 .has('graphId', gId)
                 .hasLabel('node')
            ";
                    var incomingResults = await _gremlinClient.SubmitAsync<dynamic>(incomingQuery, new Dictionary<string, object>
                    {
                        ["vId"] = node.Id.ToString(),
                        ["gId"] = graphId.ToString()
                    });

                    foreach (var sourceVertex in incomingResults)
                    {
                        var sourceNodeId = Guid.Parse(sourceVertex["id"].ToString());
                        Node sourceNode;
                        if (nodes.TryGetValue(sourceNodeId, out sourceNode))
                            node.IncomingLinks.Add(sourceNode);
                    }
                }

                // Build output string, ordered by URL
                sb.AppendLine($"Graph {graphId} — Total Nodes: {nodes.Count}");

                foreach (var node in nodes.Values.OrderBy(n => n.Url))
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
