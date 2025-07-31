using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using Graphing.Core.Models;

namespace Graphing.Core.Exporters
{
    internal class GexfGraphExporter : IGraphExporter
    {
        public string Export(IReadOnlyDictionary<string, Node> graph)
        {
            var populatedNodes = graph.Values.Where(n => n.HasData).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<gexf xmlns=\"http://www.gexf.net/1.2draft\" version=\"1.2\">");
            sb.AppendLine("<graph mode=\"static\" defaultedgetype=\"directed\">");

            // Attributes (optional metadata)
            sb.AppendLine("<attributes class=\"node\" mode=\"static\">");
            sb.AppendLine("<attribute id=\"title\" title=\"Title\" type=\"string\"/>");
            sb.AppendLine("<attribute id=\"keywords\" title=\"Keywords\" type=\"string\"/>");
            sb.AppendLine("<attribute id=\"createdat\" title=\"CreatedAt\" type=\"string\"/>");
            sb.AppendLine("</attributes>");

            // Nodes
            sb.AppendLine("<nodes>");
            foreach (var node in populatedNodes)
            {
                sb.AppendLine($"<node id=\"{Escape(node.Id)}\" label=\"{Escape(node.Title)}\">");
                sb.AppendLine("<attvalues>");
                sb.AppendLine($"<attvalue for=\"title\" value=\"{Escape(node.Title)}\"/>");
                sb.AppendLine($"<attvalue for=\"keywords\" value=\"{Escape(node.Keywords)}\"/>");
                sb.AppendLine($"<attvalue for=\"createdat\" value=\"{node.CreatedAt:o}\"/>");
                sb.AppendLine("</attvalues>");
                sb.AppendLine("</node>");
            }
            sb.AppendLine("</nodes>");

            // Edges
            sb.AppendLine("<edges>");
            int edgeId = 0;
            foreach (var node in populatedNodes)
            {
                foreach (var targetId in node.Edges)
                {
                    if (graph.TryGetValue(targetId, out var targetNode) && targetNode.HasData)
                    {
                        sb.AppendLine($"<edge id=\"e{edgeId++}\" source=\"{Escape(node.Id)}\" target=\"{Escape(targetId)}\" />");
                    }
                }
            }
            sb.AppendLine("</edges>");

            sb.AppendLine("</graph>");
            sb.AppendLine("</gexf>");

            return sb.ToString();
        }

        private string Escape(string value)
        {
            return SecurityElement.Escape(value) ?? string.Empty;
        }
    }

}
