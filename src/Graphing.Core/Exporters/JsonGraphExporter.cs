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
    internal class JsonGraphExporter : IGraphExporter
    {
        public string Export(IReadOnlyDictionary<string, Node> graph)
        {
            var pages = graph.Values.Select(p => new
            {
                url = p.Id,
                title = p.Title,
                keywords = p.Keywords,
                outgoingLinks = p.Edges.ToList()
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(pages, options);
        }
    }
}
