using System;
using Events.Core.Dtos;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Adapters.SigmaJs
{
    public class SigmaJsGraphSerializer
    {
        public static IEnumerable<SigmaGraphNodeDto> MapNodes(Node node)
        {
            // Main node
            yield return CreateNodeDto(node);

            // Outgoing linked nodes
            foreach (var linkedNode in node.OutgoingLinks)
            {
                yield return CreateNodeDto(linkedNode);
            }
        }
        public static IEnumerable<SigmaGraphEdgeDto> MapEdges(Node node)
        {
            foreach (var link in node.OutgoingLinks)
            {
                yield return new SigmaGraphEdgeDto
                {
                    Id = $"{node.Url}->{link.Url}",
                    Source = node.Url,
                    Target = link.Url
                };
            }
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


        private static double CalculateNodeSize(int incomingLinks)
        {
            const double minSize = 1.5;
            const double maxSize = 10.0;

            // Logarithmic scaling: keeps huge counts from exploding
            return minSize +
                (Math.Log10(incomingLinks + 1) * (maxSize - minSize) / Math.Log10(1000));
        }
    }
}
