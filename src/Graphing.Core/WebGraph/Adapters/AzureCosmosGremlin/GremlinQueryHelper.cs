using System;
using System.Text.Json;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin
{
    public static class GremlinQueryHelper
    {
        public static int? GetPropInt(IDictionary<string, object>? properties, string key)
        {
            var str = GetPropString(properties, key);
            if (int.TryParse(str, out var val)) return val;
            return null;
        }

        public static bool? GetPropBool(IDictionary<string, object>? properties, string key)
        {
            var str = GetPropString(properties, key);
            if (bool.TryParse(str, out var val)) return val;
            return null;
        }

        public static DateTimeOffset? GetPropDateTimeOffset(IDictionary<string, object>? properties, string key)
        {
            var str = GetPropString(properties, key);
            if (DateTimeOffset.TryParse(str, out var val)) return val;
            return null;
        }

        public static string? GetPropString(
            IDictionary<string, object>? properties,
            string key)
        {
            // 1) Guard: no props or key missing
            if (properties is null) return null;
            if (!properties.TryGetValue(key, out var rawEntry) || rawEntry is null) return null;

            // 2) If the entry is a list, take the first item
            if (rawEntry is IEnumerable<object> entryList)
            {
                var firstItem = entryList.Cast<object?>().FirstOrDefault();
                if (firstItem == null) return null;

                // 3a) Check if first item is a dictionary
                if (firstItem is IDictionary<string, object> dict)
                {
                    if (dict.TryGetValue("value", out var val))
                    {
                        return val?.ToString(); // return the value inside the dictionary
                    }
                }

                // 3b) Check if first item is a JsonElement
                else if (firstItem is JsonElement json)
                {
                    if (json.ValueKind == JsonValueKind.Object)
                    {
                        if (json.TryGetProperty("value", out var valueProp))
                        {
                            return valueProp.ToString(); // return the "value" property
                        }
                    }
                }

                // 3c) Otherwise, just return firstItem as string
                return firstItem.ToString();
            }

            // 4) Otherwise, just return the raw entry as string
            return rawEntry.ToString();
        }

        /// <summary>
        /// Extracts a list of strings from a vertex property in Cosmos/Gremlin.
        /// Handles arrays of strings, single strings, and JsonElements.
        /// </summary>
        public static List<string> GetPropStringList(IDictionary<string, object>? properties, string key)
        {
            // 1) Guard: no properties or key missing
            if (properties == null || !properties.TryGetValue(key, out var rawEntry) || rawEntry == null)
                return new List<string>();

            // 2) If the value is a list of objects
            if (rawEntry is IEnumerable<object> entryList)
            {
                var list = new List<string>();

                foreach (var item in entryList)
                {
                    if (item == null)
                        continue;

                    // 2a) Check if item is a dictionary
                    if (item is IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue("value", out var val) && val != null)
                        {
                            var strVal = val.ToString()!;

                            // If it contains commas, split into multiple entries
                            if (strVal.Contains(','))
                            {
                                var parts = strVal.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(s => s.Trim());
                                list.AddRange(parts);
                            }
                            else
                            {
                                list.Add(strVal);
                            }

                            continue; // done with this item
                        }
                    }

                    // 2b) Check if item is a JsonElement string
                    else if (item is JsonElement json)
                    {
                        if (json.ValueKind == JsonValueKind.String)
                        {
                            list.Add(json.GetString()!);
                            continue;
                        }
                    }

                    // 2c) Fallback: any other type
                    list.Add(item.ToString()!);
                }

                return list;
            }

            // 3) Fallback: single object
            return new List<string> { rawEntry.ToString()! };
        }

        public static Graph HydrateGraphFromVertex(dynamic vertex)
        {
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));

            if ((string)vertex["label"] != "graph")
                throw new InvalidOperationException("Vertex is not a Graph");

            var props = vertex["properties"] as IDictionary<string, object>;

            //TODO: consider using Nullable types in Node instead of default values here!

            return new Graph
            {
                Id = Guid.Parse(vertex["id"].ToString()),
                UserId = GetPropString(props, "userId") ?? string.Empty,
                Name = GetPropString(props, "name") ?? string.Empty,
                Description = GetPropString(props, "description") ?? string.Empty,
                Url = GetPropString(props, "url") ?? string.Empty,
                MaxDepth = GetPropInt(props, "maxDepth") ?? 1,
                MaxLinks = GetPropInt(props, "maxLinks") ?? 1,
                ExcludeExternalLinks = GetPropBool(props, "excludeExternalLinks") ?? true,
                ExcludeQueryStrings = GetPropBool(props, "excludeQueryStrings") ?? true,
                UrlMatchRegex = GetPropString(props, "urlMatchRegex") ?? string.Empty,
                TitleElementXPath = GetPropString(props, "titleElementXPath") ?? string.Empty,
                ContentElementXPath = GetPropString(props, "contentElementXPath") ?? string.Empty,
                SummaryElementXPath = GetPropString(props, "summaryElementXPath") ?? string.Empty,
                ImageElementXPath = GetPropString(props, "imageElementXPath") ?? string.Empty,
                RelatedLinksElementXPath = GetPropString(props, "relatedLinksElementXPath") ?? string.Empty,
                CreatedAt = GetPropDateTimeOffset(props, "createdAt") ?? DateTimeOffset.UtcNow,
                UserAgent = GetPropString(props, "userAgent") ?? string.Empty,
                UserAccepts = GetPropString(props, "userAccepts") ?? string.Empty
            };
        }

        public static Node HydrateNodeFromVertex(dynamic vertex, Guid graphId)
        {
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));

            if ((string)vertex["label"] != "node")
                throw new InvalidOperationException("Vertex is not a Node");

            var props = vertex["properties"] as IDictionary<string, object>;

            if (!Enum.TryParse<NodeState>(GremlinQueryHelper.GetPropString(props, "state"), out NodeState nodeState))
                throw new InvalidOperationException("Vertex has an invalid Node state.");

            //TODO: consider using Nullable types in Node instead of default values here!

            return new Node
            {
                Id = Guid.Parse(vertex["id"].ToString()),
                GraphId = graphId,
                Url = GetPropString(props, "url")!,
                Title = GetPropString(props, "title") ?? string.Empty,
                Summary = GetPropString(props, "summary") ?? string.Empty,
                ImageUrl = GetPropString(props, "imageUrl") ?? string.Empty,
                Keywords = GetPropString(props, "keywords") ?? string.Empty,
                Tags = GetPropStringList(props, "tags"),
                State = nodeState,
                RedirectedToUrl = GetPropString(props, "redirectedToUrl") ?? string.Empty,
                PopularityScore = GetPropInt(props, "popularityScore") ?? 0,
                CreatedAt = GetPropDateTimeOffset(props, "createdAt") ?? DateTimeOffset.UtcNow,
                ModifiedAt = GetPropDateTimeOffset(props, "modifiedAt") ?? DateTimeOffset.UtcNow,
                LastScheduledAt = GetPropDateTimeOffset(props, "lastScheduledAt"),
                SourceLastModified = GetPropDateTimeOffset(props, "sourceLastModified"),
                ContentFingerprint = GetPropString(props, "contentFingerprint") ?? string.Empty,
                OutgoingLinks = new HashSet<Node>(),
                IncomingLinks = new HashSet<Node>()
            };
        }
    }
}
