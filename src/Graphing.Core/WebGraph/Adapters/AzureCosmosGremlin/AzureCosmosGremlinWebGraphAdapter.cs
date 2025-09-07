using System;
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
        }

        private Node HydrateVertex(dynamic vertex, Guid graphId)
        {
            if (vertex == null) throw new ArgumentNullException(nameof(vertex));

            if ((string)vertex["label"] != "node")
                throw new InvalidOperationException("Vertex is not a Node");

            var props = vertex["properties"] as IDictionary<string, object>;

            Enum.TryParse<NodeState>(GremlinHelpers.GetPropString(props, "state"), out NodeState nodeState);
            

            //TODO: consider using Nullable types in Node instead of default values here!

            return new Node
            {
                Id = Guid.Parse(vertex["id"].ToString()),
                GraphId = graphId,
                Url = GremlinHelpers.GetPropString(props, "url")!,
                Title = GremlinHelpers.GetPropString(props, "title") ?? string.Empty,
                Summary = GremlinHelpers.GetPropString(props, "summary") ?? string.Empty,
                ImageUrl = GremlinHelpers.GetPropString(props, "imageUrl") ?? string.Empty,
                Keywords = GremlinHelpers.GetPropString(props, "keywords") ?? string.Empty,
                Tags = GremlinHelpers.GetPropStringList(props, "tags"),
                State = nodeState,
                RedirectedToUrl = GremlinHelpers.GetPropString(props, "redirectedToUrl") ?? string.Empty,
                PopularityScore = GremlinHelpers.GetPropInt(props, "popularityScore") ?? 0,
                CreatedAt = GremlinHelpers.GetPropDateTimeOffset(props, "createdAt") ?? DateTimeOffset.UtcNow,
                ModifiedAt = GremlinHelpers.GetPropDateTimeOffset(props, "modifiedAt") ?? DateTimeOffset.UtcNow,
                LastScheduledAt = GremlinHelpers.GetPropDateTimeOffset(props, "lastScheduledAt"),
                SourceLastModified = GremlinHelpers.GetPropDateTimeOffset(props, "sourceLastModified"),
                ContentFingerprint = GremlinHelpers.GetPropString(props, "contentFingerprint") ?? string.Empty,
                OutgoingLinks = new HashSet<Node>(),
                IncomingLinks = new HashSet<Node>()
            };
        }


        public override async Task<Node?> GetNodeAsync(Guid graphId, string url)
        {
            //Gremlin doesnt allow special chars in Id field,
            //therefore Guid is used as Id instead of Url

            try
            {
                // Lookup vertex by graphId + url
                const string lookupQuery = "g.V().hasLabel('node').has('graphId', gId).has('url', u)";
                var lookupParams = new Dictionary<string, object>
                {
                    ["gId"] = graphId.ToString(),
                    ["u"] = url
                };

                var results = await _gremlinClient.SubmitAsync<dynamic>(lookupQuery, lookupParams);
                var vertex = results.FirstOrDefault();
                if (vertex == null) return null;

                var node = HydrateVertex(vertex, graphId);

                // Populate outgoing links
                const string outgoingQuery = @"
                    g.V(vId)
                     .outE('linksTo')
                     .inV()
                     .hasLabel('node')";
                var outgoingResults = await _gremlinClient.SubmitAsync<dynamic>(outgoingQuery, new Dictionary<string, object>
                {
                    ["vId"] = node.Id.ToString()
                });

                foreach (var targetVertex in outgoingResults)
                {
                    node.OutgoingLinks.Add(HydrateVertex(targetVertex, graphId));
                }

                // Populate incoming links
                const string incomingQuery = @"
                    g.V(vId)
                     .inE('linksTo')
                     .outV()
                     .hasLabel('node')";
                var incomingResults = await _gremlinClient.SubmitAsync<dynamic>(incomingQuery, new Dictionary<string, object>
                {
                    ["vId"] = node.Id.ToString(),
                });

                foreach (var sourceVertex in incomingResults)
                {
                    node.IncomingLinks.Add(HydrateVertex(sourceVertex, graphId));
                }

                return node;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed GetNodeAsync for {url} in graph {graphId}", url, graphId);
                return null;
            }
        }

        public override async Task<Node> SetNodeAsync(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            // Lookup vertex by graphId + url
            const string lookupQuery = "g.V().hasLabel('node').has('graphId', gId).has('url', u).values('id')";
            var lookupParams = new Dictionary<string, object>
            {
                ["gId"] = node.GraphId.ToString(),
                ["u"] = node.Url
            };

            var idResults = await _gremlinClient.SubmitAsync<dynamic>(lookupQuery, lookupParams);
            string vertexId = idResults.FirstOrDefault()?.ToString() ?? node.Id.ToString();


            // Check for stale updates
            var storedNode = await GetNodeAsync(node.GraphId, node.Url);
            if (storedNode != null && storedNode.ModifiedAt > node.ModifiedAt)
            {
                _logger.LogDebug($"SetNodeAsync skipped for {node.Url} in GraphId: {node.GraphId} due to stale data. Incoming ModifiedAt: {node.ModifiedAt}, Stored ModifiedAt: {storedNode.ModifiedAt}");
                return storedNode;
            }

            // Upsert vertex properties
            node.ModifiedAt = DateTimeOffset.UtcNow;
            var tagsCsv = node.Tags != null ? string.Join(',', node.Tags) : string.Empty;

            var upsertQuery = @"
                g.V(vId)
                 .fold()
                 .coalesce(
                     unfold(),
                     addV('node').property('id', vId).property('graphId', gId).property('url', url)
                 )
                 .property('title', title)
                 .property('summary', summary)
                 .property('imageUrl', imageUrl)
                 .property('keywords', keywords)
                 .property('tags', tags)
                 .property('state', state)
                 .property('redirectedToUrl', redirectedToUrl)
                 .property('popularityScore', popularityScore)
                 .property('createdAt', createdAt)
                 .property('modifiedAt', modifiedAt)
                 .property('lastScheduledAt', lastScheduledAt)
                 .property('sourceLastModified', sourceLastModified)
                 .property('contentFingerprint', contentFingerprint)
            ";

            var parameters = new Dictionary<string, object>
            {
                ["vId"] = vertexId,
                ["gId"] = node.GraphId.ToString(),
                ["url"] = node.Url,
                ["title"] = node.Title ?? string.Empty,
                ["summary"] = node.Summary ?? string.Empty,
                ["imageUrl"] = node.ImageUrl ?? string.Empty,
                ["keywords"] = node.Keywords ?? string.Empty,
                ["tags"] = tagsCsv,
                ["state"] = node.State.ToString(),
                ["redirectedToUrl"] = node.RedirectedToUrl ?? string.Empty,
                ["popularityScore"] = node.PopularityScore,
                ["createdAt"] = node.CreatedAt.ToString("O"),
                ["modifiedAt"] = node.ModifiedAt.ToString("O"),
                ["lastScheduledAt"] = node.LastScheduledAt?.ToString("O") ?? "",
                ["sourceLastModified"] = node.SourceLastModified?.ToString("O") ?? "",
                ["contentFingerprint"] = node.ContentFingerprint ?? ""
            };

            try
            {
                await _gremlinClient.SubmitAsync<dynamic>(upsertQuery, parameters);
                node.Id = Guid.Parse(vertexId); // ensure node has correct internal GUID

                var output = await DumpGraphContentsAsync(node.GraphId);
                _logger.LogInformation(output);

                return node;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed SetNodeAsync for {url} in graph {graphId}", node.Url, node.GraphId);
                throw;
            }

        }

        protected async override Task<bool> AddOutgoingLinkAsync(Guid graphId, Node fromNode, Node toNode)
        {
            try
            {
                // Ensure both nodes exist
                var storedFrom = await GetNodeAsync(graphId, fromNode.Url);
                var storedTo = await GetNodeAsync(graphId, toNode.Url);

                if (storedFrom == null || storedTo == null)
                {
                    _logger.LogWarning("AddOutgoingLinkAsync skipped because one of the nodes does not exist: from={fromUrl}, to={toUrl}",
                        fromNode.Url, toNode.Url);
                    return false;
                }

                // Add edge in Cosmos/Gremlin if it does not already exist
                var edgeQuery = @"
                    g.V(fromId).hasLabel('node').has('graphId', gId).as('from')
                     .V(toId).hasLabel('node').has('graphId', gId).as('to')
                     .coalesce(
                         __.select('from').outE('linksTo').where(inV().hasId(toId)).has('graphId', gId),
                         __.addE('linksTo').from('from').to('to').property('graphId', gId)
                     )
                ";

                var parameters = new Dictionary<string, object>
                {
                    ["fromId"] = storedFrom.Id.ToString(),
                    ["toId"] = storedTo.Id.ToString(),
                    ["gId"] = graphId.ToString()
                };

                await _gremlinClient.SubmitAsync<dynamic>(edgeQuery, parameters);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add outgoing link from {fromUrl} to {toUrl} in graph {graphId}",
                    fromNode.Url, toNode.Url, graphId);
                return false;
            }
        }


        protected async override Task<bool> AddIncomingLinkAsync(Guid graphId, Node toNode, Node fromNode)
        {
            // Same as outgoing, just reversed
            return await AddOutgoingLinkAsync(graphId, fromNode, toNode);
        }

        protected async override Task ClearOutgoingLinksAsync(Guid graphId, Node node)
        {
            var query = @"
                g.V(vId)
                 .outE('linksTo')
                 .where(
                     inV().hasLabel('node').has('graphId', gId)
                 )
                 .drop()";
            await _gremlinClient.SubmitAsync<dynamic>(query, new Dictionary<string, object>
            {
                ["vId"] = node.Id.ToString(),
                ["gId"] = graphId.ToString()
            });
        }

        protected async override Task<int> GetPopularityScoreAsync(Guid graphId, Node node)
        {
            const string query = @"
                g.V(vId)
                  .bothE('linksTo')
                  .otherV()
                  .hasLabel('node')
                  .has('graphId', gId)
                  .count()
            ";

            var result = await _gremlinClient.SubmitAsync<dynamic>(query, new Dictionary<string, object>
            {
                ["vId"] = node.Id.ToString(),
                ["gId"] = graphId.ToString()
            });

            return result.FirstOrDefault() ?? 0;
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
                    var node = HydrateVertex(vertex, graphId);
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























        public override async Task<Graph?> GetGraphByIdAsync(Guid graphId)
        {
            // Only search vertices in the correct partition
            var query = "g.V().hasLabel('graph').has('graphId', graphId)";
            var parameters = new Dictionary<string, object> { ["graphId"] = graphId.ToString() };


            try
            {
                var results = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                var vertex = results.FirstOrDefault();
                if (vertex == null)
                    return null;

                // Cosmos Gremlin returns properties as a dictionary-like object
                var props = vertex["properties"] as IDictionary<string, object>;

                var graph = new Graph
                {
                    Id = Guid.Parse(vertex["id"].ToString()),
                    Name = GremlinHelpers.GetPropString(props, "name") ?? string.Empty,
                    Description = GremlinHelpers.GetPropString(props, "description") ?? string.Empty,
                    Url = GremlinHelpers.GetPropString(props, "url") ?? string.Empty,
                    MaxDepth = GremlinHelpers.GetPropInt(props, "maxDepth") ?? 1,
                    MaxLinks = GremlinHelpers.GetPropInt(props, "maxLinks") ?? 1,
                    ExcludeExternalLinks = GremlinHelpers.GetPropBool(props, "excludeExternalLinks") ?? true,
                    ExcludeQueryStrings = GremlinHelpers.GetPropBool(props, "excludeQueryStrings") ?? true,
                    UrlMatchRegex = GremlinHelpers.GetPropString(props, "urlMatchRegex") ?? string.Empty,
                    TitleElementXPath = GremlinHelpers.GetPropString(props, "titleElementXPath") ?? string.Empty,
                    ContentElementXPath = GremlinHelpers.GetPropString(props, "contentElementXPath") ?? string.Empty,
                    SummaryElementXPath = GremlinHelpers.GetPropString(props, "summaryElementXPath") ?? string.Empty,
                    ImageElementXPath = GremlinHelpers.GetPropString(props, "imageElementXPath") ?? string.Empty,
                    RelatedLinksElementXPath = GremlinHelpers.GetPropString(props, "relatedLinksElementXPath") ?? string.Empty,
                    CreatedAt = GremlinHelpers.GetPropDateTimeOffset(props, "createdAt") ?? DateTimeOffset.UtcNow,
                    UserAgent = GremlinHelpers.GetPropString(props, "userAgent") ?? string.Empty,
                    UserAccepts = GremlinHelpers.GetPropString(props, "userAccepts") ?? string.Empty
                };

                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gremlin failed to fetch graph {graphId} query: {query}", query, graphId);
                throw;
            }
        }

        public override async Task<PagedResult<Graph>> ListGraphsAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 8;

            int start = (page - 1) * pageSize;
            int end = start + pageSize;

            // Query vertices with label 'graph', ordered by createdAt
            var query = @"
                g.V().hasLabel('graph')
                 .order().by('createdAt', incr)
                 .range(start, end)
            ";
            var parameters = new Dictionary<string, object>
            {
                ["start"] = start,
                ["end"] = end
            };

            var graphs = new List<Graph>();

            try
            {
                var results = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                foreach (var result in results)
                {
                    if (result is not IDictionary<string, object> vertex)
                        continue;

                    //extract Id
                    var idText = vertex.TryGetValue("id", out var idObj) ? idObj?.ToString() : null;
                    if (string.IsNullOrWhiteSpace(idText)) continue;

                    var props = vertex.TryGetValue("properties", out var pObj)
                        ? pObj as IDictionary<string, object>
                        : null;


                    graphs.Add(new Graph
                    {
                        Id = Guid.Parse(idText),
                        Name = GremlinHelpers.GetPropString(props, "name") ?? string.Empty,
                        Description = GremlinHelpers.GetPropString(props, "description") ?? string.Empty,
                        Url = GremlinHelpers.GetPropString(props, "url") ?? string.Empty,
                        MaxDepth = GremlinHelpers.GetPropInt(props, "maxDepth") ?? 1,
                        MaxLinks = GremlinHelpers.GetPropInt(props, "maxLinks") ?? 1,
                        ExcludeExternalLinks = GremlinHelpers.GetPropBool(props, "excludeExternalLinks") ?? true,
                        ExcludeQueryStrings = GremlinHelpers.GetPropBool(props, "excludeQueryStrings") ?? true,
                        UrlMatchRegex = GremlinHelpers.GetPropString(props, "urlMatchRegex") ?? string.Empty,
                        TitleElementXPath = GremlinHelpers.GetPropString(props, "titleElementXPath") ?? string.Empty,
                        ContentElementXPath = GremlinHelpers.GetPropString(props, "contentElementXPath") ?? string.Empty,
                        SummaryElementXPath = GremlinHelpers.GetPropString(props, "summaryElementXPath") ?? string.Empty,
                        ImageElementXPath = GremlinHelpers.GetPropString(props, "imageElementXPath") ?? string.Empty,
                        RelatedLinksElementXPath = GremlinHelpers.GetPropString(props, "relatedLinksElementXPath") ?? string.Empty,
                        CreatedAt = GremlinHelpers.GetPropDateTimeOffset(props, "createdAt") ?? DateTimeOffset.UtcNow,
                        UserAgent = GremlinHelpers.GetPropString(props, "userAgent") ?? string.Empty,
                        UserAccepts = GremlinHelpers.GetPropString(props, "userAccepts") ?? string.Empty
                    });

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gremlin failed for query: {query}", query);
            }


            // Query total count
            var countQuery = "g.V().hasLabel('graph').count()";
            var totalCount = 0;

            try
            {
                var countResult = await _gremlinClient.SubmitWithSingleResultAsync<int>(countQuery);
                totalCount = countResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gremlin query failed to list graphs query: {query}", countQuery);
            }

            return new PagedResult<Graph>(
                graphs,
                (int)totalCount,
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

            var query = @"
                g.addV('graph')
                 .property('id', id)
                 .property('graphId', graphId)
                 .property('name', name)
                 .property('description', description)
                 .property('url', url)
                 .property('maxDepth', maxDepth)
                 .property('maxLinks', maxLinks)
                 .property('excludeExternalLinks', excludeExternalLinks)
                 .property('excludeQueryStrings', excludeQueryStrings)
                 .property('urlMatchRegex', urlMatchRegex)
                 .property('titleElementXPath', titleElementXPath)
                 .property('contentElementXPath', contentElementXPath)
                 .property('summaryElementXPath', summaryElementXPath)
                 .property('imageElementXPath', imageElementXPath)
                 .property('relatedLinksElementXPath', relatedLinksElementXPath)
                 .property('createdAt', createdAt)
                 .property('userAgent', userAgent)
                 .property('userAccepts', userAccepts)
            ";

            var parameters = new Dictionary<string, object>
            {
                ["id"] = graph.Id.ToString(),
                ["graphId"] = graph.Id.ToString(),
                ["name"] = graph.Name,
                ["description"] = graph.Description,
                ["url"] = graph.Url,
                ["maxDepth"] = graph.MaxDepth,
                ["maxLinks"] = graph.MaxLinks,
                ["excludeExternalLinks"] = graph.ExcludeExternalLinks,
                ["excludeQueryStrings"] = graph.ExcludeQueryStrings,
                ["urlMatchRegex"] = graph.UrlMatchRegex,
                ["titleElementXPath"] = graph.TitleElementXPath,
                ["contentElementXPath"] = graph.ContentElementXPath,
                ["summaryElementXPath"] = graph.SummaryElementXPath,
                ["imageElementXPath"] = graph.ImageElementXPath,
                ["relatedLinksElementXPath"] = graph.RelatedLinksElementXPath,
                ["createdAt"] = graph.CreatedAt.ToString("O"),
                ["userAgent"] = graph.UserAgent,
                ["userAccepts"] = graph.UserAccepts
            };

            try
            {
                await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gremlin failed to create graph {graph.Id} for query: {query}", query, graph.Id);
                throw;
            }
        }

        public override async Task<Graph?> DeleteGraphAsync(Guid graphId)
        {
            // Fetch the graph first so we can return it after deletion
            var graph = await GetGraphByIdAsync(graphId);
            if (graph == null) return null;

            // Delete the vertex using partition key to ensure isolation
            var query = "g.V().hasLabel('graph').has('graphId', graphId).drop()";
            var parameters = new Dictionary<string, object> { ["graphId"] = graphId.ToString() };


            try
            {
                await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                // Cosmos Gremlin `.drop()` returns an empty result set, so success = no exception
                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gremlin failed to delete graph {graphId} query: {query}", query, graphId);
                throw;
            }
        }

        public override async Task<Graph?> UpdateGraphAsync(Graph graph)
        {
            // Update properties of the graph vertex using partition key for isolation
            var query = @"
                g.V().hasLabel('graph').has('graphId', graphId)
                 .property('name', name)
                 .property('description', description)
                 .property('url', url)
                 .property('maxDepth', maxDepth)
                 .property('maxLinks', maxLinks)
                 .property('excludeExternalLinks', excludeExternalLinks)
                 .property('excludeQueryStrings', excludeQueryStrings)
                 .property('urlMatchRegex', urlMatchRegex)
                 .property('titleElementXPath', titleElementXPath)
                 .property('contentElementXPath', contentElementXPath)
                 .property('summaryElementXPath', summaryElementXPath)
                 .property('imageElementXPath', imageElementXPath)
                 .property('relatedLinksElementXPath', relatedLinksElementXPath)
                 .property('createdAt', createdAt)
                 .property('userAgent', userAgent)
                 .property('userAccepts', userAccepts)
            ";

            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = graph.Id.ToString(),
                ["name"] = graph.Name,
                ["description"] = graph.Description,
                ["url"] = graph.Url,
                ["maxDepth"] = graph.MaxDepth,
                ["maxLinks"] = graph.MaxLinks,
                ["excludeExternalLinks"] = graph.ExcludeExternalLinks,
                ["excludeQueryStrings"] = graph.ExcludeQueryStrings,
                ["urlMatchRegex"] = graph.UrlMatchRegex,
                ["titleElementXPath"] = graph.TitleElementXPath,
                ["contentElementXPath"] = graph.ContentElementXPath,
                ["summaryElementXPath"] = graph.SummaryElementXPath,
                ["imageElementXPath"] = graph.ImageElementXPath,
                ["relatedLinksElementXPath"] = graph.RelatedLinksElementXPath,
                ["createdAt"] = graph.CreatedAt.ToString("O"),
                ["userAgent"] = graph.UserAgent,
                ["userAccepts"] = graph.UserAccepts
            };

            try
            {
                await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                // Fetch the updated vertex fresh from DB
                return await GetGraphByIdAsync(graph.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gremlin failed to update graph {graph.Id} query: {query}", query, graph.Id);
                throw;
            }
        }



        public override Task<IEnumerable<Node>> GetMostPopularNodes(Guid graphId, int topN)
        {
            throw new NotImplementedException();
        }

        public override Task<int> TotalPopulatedNodesAsync(Guid graphID)
        {
            throw new NotImplementedException();
        }

        public override Task CleanupOrphanedNodesAsync(Guid graphId)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<Node>> TraverseGraphAsync(Guid graphId, string startUrl, int maxDepth, int? maxNodes = null)
        {
            throw new NotImplementedException();
        }
    }
}
