using System;
using System.Text.Json;

namespace Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin
{
    public static class GremlinHelpers
    {
        public static string? GetPropString(
            IDictionary<string, object>? properties,
            string key)
        {
            // 1) Guard: no props or key missing
            if (properties is null) return null;
            if (!properties.TryGetValue(key, out var rawEntry) || rawEntry is null) return null;

            if (rawEntry is IEnumerable<object> entryList)
            {
                var firstItem = entryList.Cast<object?>().FirstOrDefault();
                if (firstItem == null) return null;

                switch (firstItem)
                {
                    case IDictionary<string, object> dict when dict.TryGetValue("value", out var val):
                        return val?.ToString();
                    case JsonElement json when json.ValueKind == JsonValueKind.Object && json.TryGetProperty("value", out var valueProp):
                        return valueProp.ToString();
                    default:
                        return firstItem.ToString();
                }
            }

            return rawEntry.ToString();
        }

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

        /// <summary>
        /// Extracts a list of strings from a multi-property vertex in Cosmos/Gremlin.
        /// Handles IEnumerable of dictionaries or JsonElements.
        /// </summary>
        public static List<string> GetPropStringList(IDictionary<string, object>? properties, string key)
        {
            if (properties == null || !properties.TryGetValue(key, out var rawEntry) || rawEntry == null)
                return new List<string>();

            if (rawEntry is IEnumerable<object> entryList)
            {
                var list = new List<string>();
                foreach (var item in entryList.Cast<object?>())
                {
                    if (item == null) continue;

                    switch (item)
                    {
                        case IDictionary<string, object> dict when dict.TryGetValue("value", out var val):
                            if (val != null) list.Add(val.ToString()!);
                            break;
                        case JsonElement json when json.ValueKind == JsonValueKind.String:
                            list.Add(json.GetString()!);
                            break;
                        default:
                            list.Add(item.ToString()!);
                            break;
                    }
                }
                return list;
            }

            return new List<string> { rawEntry.ToString()! };
        }
    }
}
