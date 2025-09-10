using System;
using System.Text.Json;
using Graphing.Core.WebGraph.Models;
using Gremlin.Net.Driver;
using Microsoft.Extensions.Logging;

namespace Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin
{
    public class GremlinQueryProvider : IGremlinQueryProvider
    {
        private readonly GremlinClient _gremlinClient;
        private readonly ILogger _logger;

        public GremlinQueryProvider(ILogger logger, GremlinClient gremlinClient)
        {
            _gremlinClient = gremlinClient;
            _logger = logger;
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
                var results = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                return results.FirstOrDefault();
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
                var vertices = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
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
                return await _gremlinClient.SubmitWithSingleResultAsync<int>(query);
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
                var results = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                return results.FirstOrDefault();
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
                await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                // Cosmos Gremlin mutation returns an empty result set,
                // so success = no exception
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
                await _gremlinClient.SubmitAsync<dynamic>(deleteNodesQuery, parameters);
                await _gremlinClient.SubmitAsync<dynamic>(deleteGraphQuery, parameters);

                // Cosmos Gremlin `.drop()` returns an empty result set,
                // so success = no exception
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
                var results = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                return results.FirstOrDefault();
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
                var edges = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                var results = edges.Select(e =>
                {
                    dynamic node = e["node"];
                    dynamic other = e["other"];
                    dynamic edge = e["edge"];

                    bool isOutgoing = true; // default fallback

                    if (edge is IDictionary<string, object> edgeDict && edgeDict.TryGetValue("outV", out var outVObj))
                    {
                        var outV = outVObj.ToString();
                        isOutgoing = outV == ((IDictionary<string, object>)node)["id"].ToString();
                    }

                    return (source: node, target: other, isOutgoing);
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

            node.ModifiedAt = DateTimeOffset.UtcNow;

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
                await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                // Cosmos Gremlin mutation returns an empty result set,
                // so success = no exception
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
            var edgeQuery = @"
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
                await _gremlinClient.SubmitAsync<dynamic>(edgeQuery, parameters);
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

            try
            {
                await _gremlinClient.SubmitAsync<dynamic>(query, new Dictionary<string, object>
                {
                    ["vertexId"] = node.Id.ToString(),
                    ["graphId"] = graphId.ToString()
                });
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
                await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
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
                var result = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);

                return result.FirstOrDefault() ?? 0;
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
                return await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
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
                var results = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                var count = results.FirstOrDefault();

                return Convert.ToInt64(count);
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
                var results = await _gremlinClient.SubmitAsync<dynamic>(query, parameters);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get node vertext subgraph for vertex {vertexId} graph {graphId}", vertexId, graphId);
                throw;
            }
        }
    }
}
