using System;
using Graphing.Core.WebGraph.Models;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin
{
    public class GremlinQueryProvider : IGremlinQueryProvider
    {
        private readonly GremlinClient _gremlinClient;
        private readonly ILogger _logger;
        private readonly int _maxQueryRetries;

        public GremlinQueryProvider(ILogger logger, GremlinClient gremlinClient, int maxQueryRetries)
        {
            _gremlinClient = gremlinClient;
            _logger = logger;
            _maxQueryRetries = maxQueryRetries;
        }

        // Graph Operations

        public async Task<dynamic?> GetGraphVertexAsync(Guid graphId)
        {
            var query = "g.V().hasLabel('graph').has('graphId', graphId)";

            var parameters = new Dictionary<string, object> { 
                ["graphId"] = graphId.ToString() 
            };

            try
            {
                var vertex = await ExecuteListQueryAsync(query, parameters,
                    operationName: "GetGraphVertexAsync");

                return vertex.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch vertex {graphId} query: {query}", graphId, query);
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> ListGraphVerticesAsync(int start, int end)
        {
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

            try
            {
                var vertices = await ExecuteListQueryAsync(query, parameters,
                    operationName: "ListGraphVerticesAsync");

                return vertices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch vertices query: {query}", query);
                throw;
            }
        }

        public async Task<int> CountGraphVerticesAsync()
        {
            var query = "g.V().hasLabel('graph').count()";

            try
            {
                var count = await ExecuteScalarQueryAsync<int>(query, null,
                    operationName: "CountGraphVerticesAsync");

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count graph vertices query: {query}", query);
                throw;
            }
        }

        public async Task<dynamic?> CreateGraphVertexAsync(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

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
                var vertex = await ExecuteListQueryAsync(query, parameters,
                    operationName: "CreateGraphVertexAsync");

                return vertex.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create graph {graphId} for query: {query}", graph.Id, query);
                throw;
            }
        }

        public async Task UpdateGraphVertexAsync(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

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
                await ExecuteCommandAsync(query, parameters,
                    operationName: "UpdateGraphVertexAsync"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update graph {graphId} query: {query}", graph.Id, query);
                throw;
            }
        }

        public async Task DeleteGraphVertexAsync(Guid graphId)
        {
            // Delete all nodes
            var deleteNodesQuery = @"
                g.V()
                 .hasLabel('node')
                 .has('graphId', graphId)
                 .drop()";

            // Delete the graph vertex itself
            var deleteGraphQuery = @"
                g.V()
                 .hasLabel('graph')
                 .has('graphId', graphId)
                 .drop()";

            var parameters = new Dictionary<string, object> { 
                ["graphId"] = graphId.ToString() 
            };

            try
            {
                await ExecuteCommandAsync(deleteNodesQuery, parameters,
                    operationName: "DeleteGraphVertexAsync"
                );

                await ExecuteCommandAsync(deleteGraphQuery, parameters,
                    operationName: "DeleteGraphVertexAsync"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete graph {graphId}", graphId);
                throw;
            }
        }


        // Node Operations

        public async Task<dynamic?> GetNodeVertexAsync(Guid graphId, string url)
        {
            var query = "g.V().hasLabel('node').has('graphId', graphId).has('url', url)";

            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = graphId.ToString(),
                ["url"] = url
            };

            try
            {
                var nodes = await ExecuteListQueryAsync(query, parameters,
                    operationName: "GetNodeVertexAsync");

                return nodes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get vertext for {url} in graph {graphId}", url, graphId);
                throw;
            }
        }

        public async Task<IEnumerable<(dynamic source, dynamic target, bool isOutgoing)>> 
            GetNodeVerticesEdgesAsync(Guid graphId, IEnumerable<string> urls)
        {
            // 1. Find all vertices (nodes) in the given graph with the given URLs.
            // 2. From each node, traverse both incoming and outgoing 'linksTo' edges.
            // 3. For each edge, also capture the "other" vertex at the far end.
            // 4. Select (node, edge, other) triples for mapping
            var query = @"
                g.V().hasLabel('node').has('graphId', graphId).has('url', within(urls))
                  .as('node')
                  .bothE('linksTo').as('edge')
                  .otherV().as('other')
                  .select('node','edge','other')
            ";

            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = graphId.ToString(),
                ["urls"] = urls.ToList()
            };

            try
            {
                var edges = await ExecuteListQueryAsync(query, parameters,
                    operationName: "GetNodeVerticesEdgesAsync");
               
                // Map results into (source, target, isOutgoing) tuples.
                var results = edges.Select(e =>
                {
                    // Extract the projected elements from the query.
                    dynamic node = e["node"];
                    dynamic other = e["other"];
                    dynamic edge = e["edge"];

                    // Get edge details: outV = source vertex id, inV = target vertex id.
                    var edgeDict = (IDictionary<string, object>)edge;
                    var outV = edgeDict["outV"].ToString();
                    var inV = edgeDict["inV"].ToString();

                    // The start node id
                    var nodeId = ((IDictionary<string, object>)node)["id"].ToString();

                    bool isOutgoing;
                    dynamic source;
                    dynamic target;

                    if (nodeId == outV)
                    {
                        // If the current node is the edge's "outV", the edge goes outwards:
                        // node → other
                        isOutgoing = true;
                        source = node;
                        target = other;
                    }
                    else
                    {
                        // Otherwise, the edge comes into the current node:
                        // other → node
                        isOutgoing = false;
                        source = other;
                        target = node;
                    }

                    // Return tuple describing this relationship.
                    return (source, target, isOutgoing);
                });

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get vertex edges for {urls} in graph {graphId}", urls, graphId);
                throw;
            }
        }

        public async Task UpsertNodeVertexAsync(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var query = @"
                g.V(vertexId)
                 .fold()
                 .coalesce(
                     unfold(),
                     addV('node').property('id', vertexId).property('graphId', graphId).property('url', url)
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
                 .property('contentFingerprint', contentFingerprint)";

            var parameters = new Dictionary<string, object>
            {
                ["vertexId"] = node.Id.ToString(),
                ["graphId"] = node.GraphId.ToString(),
                ["url"] = node.Url,
                ["title"] = node.Title,
                ["summary"] = node.Summary,
                ["imageUrl"] = node.ImageUrl,
                ["keywords"] = node.Keywords,
                ["tags"] = string.Join(",", node.Tags ?? Array.Empty<string>()),
                ["state"] = node.State.ToString(),
                ["redirectedToUrl"] = node.RedirectedToUrl,
                ["popularityScore"] = node.PopularityScore,
                ["createdAt"] = node.CreatedAt.ToString("O"),
                ["modifiedAt"] = node.ModifiedAt.ToString("O"),
                ["lastScheduledAt"] = node.LastScheduledAt?.ToString("O") ?? "",
                ["sourceLastModified"] = node.SourceLastModified?.ToString("O") ?? "",
                ["contentFingerprint"] = node.ContentFingerprint
            };

            try
            {
                await ExecuteCommandAsync(query, parameters,
                    operationName: "UpsertNodeVertexAsync"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update node vertex {id} in graph {graphId}", node.Id, node.GraphId);
                throw;
            }
        }

        public async Task AddNodeVertexEdgeAsync(Node fromNode, Node toNode, Guid graphId)
        {
            // using coalesce to add edge if it does not already exist
            var query = @"
                g.V(fromId).hasLabel('node').has('graphId', graphId).as('from')
                 .V(toId).hasLabel('node').has('graphId', graphId).as('to')
                 .coalesce(
                     __.select('from').outE('linksTo').where(inV().hasId(toId)).has('graphId', graphId),
                     __.addE('linksTo').from('from').to('to').property('graphId', graphId)
                 )";

            var parameters = new Dictionary<string, object>
            {
                ["fromId"] = fromNode.Id.ToString(),
                ["toId"] = toNode.Id.ToString(),
                ["graphId"] = graphId.ToString()
            };

            try
            {
                await ExecuteCommandAsync(query, parameters,
                    operationName: "AddNodeVertexEdgeAsync"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add edge from vertex {fromNodeUrl} to {toNodeUrl} in graph {graphId}", fromNode.Url, toNode.Url, graphId);
                throw;
            }
        }

        public async Task RemoveNodeVertexEdgesAsync(Guid graphId, Node node)
        {
            var query = @"
                g.V(vertexId)
                 .outE('linksTo')
                 .where(
                     inV().hasLabel('node').has('graphId', graphId)
                 )
                 .drop()";

            var parameters = new Dictionary<string, object>
            {
                ["vertexId"] = node.Id.ToString(),
                ["graphId"] = graphId.ToString()
            };

            try
            {
                await ExecuteCommandAsync(query, parameters,
                    operationName: "RemoveNodeVertexEdgesAsync"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove edges for node {nodeId} graph {graphId}", node.Id, graphId);
                throw;
            }
        }

        public async Task RemoveOrphanedNodeVerticesAsync(Guid graphId)
        {
            var query = @"
                g.V()
                 .hasLabel('node')
                 .has('graphId', graphId)
                 .not(__.inE())
                 .has('state', within('Redirected','Dummy'))
                 .drop()";

            var parameters = new Dictionary<string, object> { 
                ["graphId"] = graphId.ToString() 
            };

            try
            {
                await ExecuteCommandAsync(query, parameters, 
                    operationName: "RemoveOrphanedNodeVerticesAsync"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove orphaned nodes for graph {graphId}", graphId);
                throw;
            }
        }

        public async Task<int> CountNodeVertexEdgesAsync(Guid graphId, Node node)
        {
            const string query = @"
                g.V(vertexId)
                  .bothE('linksTo')
                  .otherV()
                  .hasLabel('node')
                  .has('graphId', graphId)
                  .count()
            ";

            var parameters = new Dictionary<string, object>
            {
                ["vertexId"] = node.Id.ToString(),
                ["graphId"] = graphId.ToString()
            };

            try
            {
                var count = await ExecuteScalarQueryAsync<int>(query, parameters,
                    operationName: "CountNodeVertexEdgesAsync"
                );

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count edges of node vertex {nodeId} graph {graphId}", node.Id, graphId);
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> GetNodeVerticesByCreationDateAscAsync(Guid graphId, int topN)
        {
            // Cosmos Gremlin only supports Asc order
            var query = @"
                g.V()
                 .hasLabel('node')
                 .has('graphId', graphId)
                 .has('state', 'Populated')
                 .order()
                   .by('createdAt')
                 .limit(topN)";

            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = graphId.ToString(),
                ["topN"] = topN
            };

            try
            {
                var nodes = await ExecuteListQueryAsync(query, parameters,
                    operationName: "GetNodeVerticesByCreationDateAscAsync");

                return nodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch node vertices from graph {graphId}", graphId);
                return Enumerable.Empty<dynamic>();
            }
        }

        public async Task<long> CountNodeVerticesPopulatedAsync(Guid graphId)
        {
            var query = "g.V().hasLabel('node').has('graphId', graphId).has('state', 'Populated').count()";

            var parameters = new Dictionary<string, object> {
                ["graphId"] = graphId.ToString()
            };

            try
            {
                var count = await ExecuteScalarQueryAsync<long>(query, parameters, 
                    operationName: "CountNodeVerticesPopulatedAsync"
                );

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to count populated node vertices for graph {graphId}", graphId);
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> GetNodeVertexSubgraphAsync(Guid graphId, string vertexId, int maxDepth, int? maxNodes = null)
        {
            var query = $@"
                g.V(vertexId)
                 .has('graphId', graphId)
                 .emit()
                 .repeat(bothE('linksTo').subgraph('sg').otherV())
                   .times({maxDepth})
                 .cap('sg')
            ";
            if (maxNodes.HasValue)
                query += $".limit({maxNodes.Value})";

            var parameters = new Dictionary<string, object>
            {
                ["graphId"] = graphId.ToString(),
                ["vertexId"] = vertexId
            };

            try
            {
                var nodes = await ExecuteListQueryAsync(query, parameters, 
                    operationName: "GetNodeVertexSubgraphAsync");

                return nodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get node vertext subgraph for vertex {vertexId} graph {graphId}", vertexId, graphId);
                throw;
            }
        }


        /// <summary>
        /// Returns a List<dynamic>
        /// </summary>
        private async Task<List<dynamic>> ExecuteListQueryAsync(
            string query,
            Dictionary<string, object>? parameters,
            string operationName)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var resultSet = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                return resultSet.ToList();
            }, operationName);
        }

        /// <summary>
        /// Returns a single scalar (int, long, etc.)
        /// </summary>
        private async Task<T> ExecuteScalarQueryAsync<T>(
            string query,
            Dictionary<string, object>? parameters,
            string operationName)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var resultSet = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                return (T)Convert.ChangeType(resultSet.FirstOrDefault(), typeof(T));
            }, operationName);
        }

        /// <summary>
        /// Runs a mutation insert/update/delete (no return value)
        /// </summary>
        private async Task ExecuteCommandAsync(
            string query,
            Dictionary<string, object>? parameters,
            string operationName)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                return true; // dummy value just to satisfy generic
            }, operationName);
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            var rng = new Random();

            for (int attempt = 1; attempt <= _maxQueryRetries; attempt++)
            {
                try
                {
                    var result = await operation();

                    if (attempt > 1)
                    {
                        _logger.LogDebug(
                            "{operationName} succeeded on retry attempt {attempt}/{maxRetries}",
                            operationName, attempt, _maxQueryRetries);
                    }

                    return result;
                }
                catch (ResponseException ex)
                {
                    var (statusCode, subStatusCode, retryAfterMs) = ParseExceptionMessage(ex.Message);

                    if (statusCode == "TooManyRequests" || statusCode == "PreconditionFailed")
                    {
                        // Use server-provided retry if available, else fallback
                        int waitMs = retryAfterMs ?? (200 * attempt + rng.Next(0, 100));
                        var wait = TimeSpan.FromMilliseconds(Math.Min(5000, waitMs));

                        _logger.LogDebug(
                            "Retryable error during {operationName}, attempt {attempt}/{maxRetries}. " +
                            "StatusCode={statusCode}, SubStatusCode={subStatusCode}. Waiting {wait} before retrying.",
                            operationName, attempt, _maxQueryRetries,
                            statusCode, subStatusCode, wait);

                        await Task.Delay(wait);
                        continue;
                    }

                    throw;
                }
            }

            throw new Exception($"Max retry attempts ({_maxQueryRetries}) exceeded for {operationName}");
        }

        private static (string? statusCode, string? subStatusCode, int? retryAfterMs) ParseExceptionMessage(string message)
        {
            string? statusCode = null;
            string? subStatusCode = null;
            int? retryAfterMs = null;

            // Scan for StatusCode / SubStatusCode
            foreach (var line in message.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("StatusCode =")) statusCode = trimmed["StatusCode =".Length..].Trim();
                if (trimmed.StartsWith("SubStatusCode =")) subStatusCode = trimmed["SubStatusCode =".Length..].Trim();
            }

            // Scan for RetryAfterInMs anywhere in the message
            const string retryKey = "\"RetryAfterInMs\"";
            var retryIndex = message.IndexOf(retryKey, StringComparison.OrdinalIgnoreCase);
            if (retryIndex >= 0)
            {
                // Find the first number after the key
                var colonIndex = message.IndexOf(':', retryIndex);
                if (colonIndex >= 0)
                {
                    var start = colonIndex + 1;
                    // Skip any quotes or whitespace
                    while (start < message.Length && (message[start] == '"' || char.IsWhiteSpace(message[start])))
                        start++;

                    int end = start;
                    while (end < message.Length && char.IsDigit(message[end]))
                        end++;

                    if (int.TryParse(message[start..end], out var ms))
                        retryAfterMs = ms;
                }
            }

            return (statusCode, subStatusCode, retryAfterMs);
        }

    }

}
