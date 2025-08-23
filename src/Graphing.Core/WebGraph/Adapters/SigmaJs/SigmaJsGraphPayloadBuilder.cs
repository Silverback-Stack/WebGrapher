using Events.Core.Dtos;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Adapters.SigmaJs
{
    public class SigmaJsGraphPayloadBuilder
    {
        public static SigmaGraphPayloadDto BuildPayload(IEnumerable<Node> nodes, Guid graphId)
        {
            var sigmaNodes = new List<SigmaGraphNodeDto>();
            var edgeSet = new HashSet<string>();
            var edges = new List<SigmaGraphEdgeDto>();

            foreach (var node in nodes)
            {
                var (nodesToSend, edgesToSend) = SigmaJsGraphSerializer.GetPopulationDelta(node);

                // Add nodes directly
                sigmaNodes.AddRange(nodesToSend);

                // Add edges if not already added
                foreach (var e in edgesToSend)
                {
                    if (edgeSet.Add(e.Id))
                        edges.Add(e);
                }
            }

            return new SigmaGraphPayloadDto
            {
                GraphId = graphId,
                Nodes = sigmaNodes,
                Edges = edges
            };
        }

        // Overload for a single node
        public static SigmaGraphPayloadDto BuildPayload(Node node)
            => BuildPayload(new[] { node }, node.GraphId);
    }
}
