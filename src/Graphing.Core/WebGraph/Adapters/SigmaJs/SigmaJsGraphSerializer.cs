using System;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Adapters.SigmaJs
{
    public class SigmaJsGraphSerializer
    {
        private static readonly Random _rand = new Random();

        public static (SigmaNode node, List<SigmaEdge> edges) ConvertToSigma(Node node)
        {
            var centerId = node.Url;

            var sigmaNode = new SigmaNode
            {
                Id = centerId,
                Label = node.Url,
                Size = 1.5,
                Color = node.State switch
                {
                    NodeState.Populated => "#4caf50",
                    NodeState.Redirected => "#ff9800",
                    _ => "#9e9e9e",
                }
            };

            var sigmaEdges = node.OutgoingLinks
                .Select(target => new SigmaEdge
                {
                    Id = $"{centerId}->{target.Url}",
                    Source = centerId,
                    Target = target.Url,
                    Color = "#90caf9"
                })
                .ToList();

            return (sigmaNode, sigmaEdges);
        }

    }
}
