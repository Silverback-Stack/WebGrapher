using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Graphing.Core.Models;

namespace Graphing.Core.Exporters
{
    internal class GJsonGraphExporter : IGraphExporter
    {
        public string Export(IReadOnlyDictionary<string, Node> graph)
        {
            // step 1: get all nodes with real data
            var realNodes = graph.Values
                .Where(p => !string.IsNullOrWhiteSpace(p.Title) || !string.IsNullOrWhiteSpace(p.Keywords))
                .ToDictionary(p => p.Id, p => p);

            // step 2: build node list
            var nodes = realNodes.Values.Select(p => new
            {
                key = p.Id,
                attributes = new
                {
                    title = p.Title,
                    keywords = p.Keywords,
                    sourceLastModified = p.SourceLastModifed?.ToString("o")
                }
            }).ToList();

            // step 3: build edges, only if target node also has data
            var edges = new List<object>();

            foreach (var sourceNode in realNodes.Values)
            {
                foreach (var targetUrl in sourceNode.Edges)
                {
                    if (realNodes.ContainsKey(targetUrl))
                    {
                        edges.Add(new { source = sourceNode.Id, target = targetUrl });
                    }
                }
            }

            var graphology = new { nodes, edges };
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(graphology, options);
        }
    }
}
