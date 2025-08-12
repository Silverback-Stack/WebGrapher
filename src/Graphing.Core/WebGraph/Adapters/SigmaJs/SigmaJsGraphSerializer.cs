using System;
using Events.Core.Dtos;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Adapters.SigmaJs
{
    public class SigmaJsGraphSerializer
    {
        public static IEnumerable<SigmaGraphNodeDto> MapNodes(Node node)
        {
            // Always include the main node if populated
            if (node.State == NodeState.Populated)
                yield return CreateNodeDto(node);

            // Include outgoing links if they point to populated nodes (after redirect resolution)
            foreach (var linkedNode in node.OutgoingLinks
                .Select(ResolveRedirect)
                .Where(n => n.State == NodeState.Populated))
            {
                yield return CreateNodeDto(linkedNode);
            }

            // Include incoming links if they are populated
            foreach (var sourceNode in node.IncomingLinks
                .Select(ResolveRedirect)
                .Where(n => n.State == NodeState.Populated))
            {
                yield return CreateNodeDto(sourceNode);
            }
        }

        public static IEnumerable<SigmaGraphEdgeDto> MapEdges(Node node)
        {
            // Outgoing edges to populated nodes
            foreach (var target in node.OutgoingLinks
                .Select(ResolveRedirect)
                .Where(n => n.State == NodeState.Populated))
            {
                yield return CreateEdgeDto(node, target);
            }

            // Incoming edges from populated nodes
            foreach (var source in node.IncomingLinks
                .Select(ResolveRedirect)
                .Where(n => n.State == NodeState.Populated))
            {
                yield return CreateEdgeDto(source, node);
            }
        }

        private static Node ResolveRedirect(Node node)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (node.State == NodeState.Redirected && node.OutgoingLinks.Any())
            {
                if (!visited.Add(node.Url))
                    break;
                node = node.OutgoingLinks.First();
            }
            return node;
        }

        private static SigmaGraphNodeDto CreateNodeDto(Node node)
        {
            return new SigmaGraphNodeDto
            {
                Id = node.Url,
                Label = node.Title,
                Size = CalculateNodeSize(node.IncomingLinkCount),
                State = node.State.ToString(),
                Keywords = node.Keywords,
                Tags = node.Tags,
                CreatedAt = node.CreatedAt,
                SourceLastModified = node.SourceLastModified
            };
        }

        private static SigmaGraphEdgeDto CreateEdgeDto(Node source, Node target)
        {
            return new SigmaGraphEdgeDto
            {
                Id = $"{source.Url}->{target.Url}",
                Source = source.Url,
                Target = target.Url
            };
        }

        private static double CalculateNodeSize(int incomingLinks)
        {
            const double minSize = 10;
            const double maxSize = 20;

            // Logarithmic scaling: keeps huge counts from exploding
            return minSize +
                (Math.Log10(incomingLinks + 1) * (maxSize - minSize) / Math.Log10(1000));
        }
    }
}
