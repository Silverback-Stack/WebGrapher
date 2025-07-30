using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Graphing.Core.Models;

namespace Graphing.Core.Exporters
{
    internal class GraphMlGraphExporter : IGraphExporter
    {
        public string Export(IReadOnlyDictionary<string, Node> graph)
        {
            // get nodes with real data
            var realNodes = graph.Values
                .Where(p => p.HasData)
                .ToDictionary(p => p.Id, p => p);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\">");

            // optionally define data keys for node attributes
            sb.AppendLine("  <key id=\"title\" for=\"node\" attr.name=\"title\" attr.type=\"string\"/>");
            sb.AppendLine("  <key id=\"keywords\" for=\"node\" attr.name=\"keywords\" attr.type=\"string\"/>");
            sb.AppendLine("  <key id=\"sourceLastModified\" for=\"node\" attr.name=\"sourceLastModified\" attr.type=\"string\"/>");

            sb.AppendLine("  <graph id=\"G\" edgedefault=\"directed\">");

            foreach (var node in realNodes.Values)
            {
                sb.AppendLine($"    <node id=\"{SecurityElement.Escape(node.Id)}\">");
                sb.AppendLine($"      <data key=\"title\">{SecurityElement.Escape(node.Title ?? "")}</data>");
                sb.AppendLine($"      <data key=\"keywords\">{SecurityElement.Escape(node.Keywords ?? "")}</data>");
                sb.AppendLine($"      <data key=\"sourceLastModified\">{node.SourceLastModifed:o}</data>");
                sb.AppendLine("    </node>");
            }

            foreach (var sourceNode in realNodes.Values)
            {
                foreach (var targetUrl in sourceNode.Edges)
                {
                    if (realNodes.ContainsKey(targetUrl))
                    {
                        sb.AppendLine($"    <edge source=\"{SecurityElement.Escape(sourceNode.Id)}\" target=\"{SecurityElement.Escape(targetUrl)}\" />");
                    }
                }
            }

            sb.AppendLine("  </graph>");
            sb.AppendLine("</graphml>");
            return sb.ToString();
        }
    }
}
