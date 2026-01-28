using Events.Core.Dtos;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Infrastructure.WebGraph.Serializers.SigmaJs
{
    public class SigmaJsGraphPayloadSerializer : IWebGraphPayloadSerializer
    {
        private readonly SigmaJsGraphSerializer _serializer;

        public SigmaJsGraphPayloadSerializer(SigmaJsGraphSettings settings)
        {
            _serializer = new SigmaJsGraphSerializer(settings);
        }

        public SigmaGraphPayloadDto Serialize(IEnumerable<Node> nodes, Guid graphId)
        {
            var sigmaNodes = new List<SigmaGraphNodeDto>();
            var edgeSet = new HashSet<string>();
            var edges = new List<SigmaGraphEdgeDto>();

            foreach (var node in nodes)
            {
                var (nodesToSend, edgesToSend) = _serializer.GetPopulationDelta(node);

                // Add nodes directly
                sigmaNodes.AddRange(nodesToSend);

                // Add edges if not already added
                foreach (var e in edgesToSend)
                    if (edgeSet.Add(e.Id))
                        edges.Add(e);
            }

            return new SigmaGraphPayloadDto
            {
                GraphId = graphId,
                Nodes = sigmaNodes,
                Edges = edges
            };
        }

        public SigmaGraphPayloadDto Serialize(Node node)
            => Serialize(new[] { node }, node.GraphId);

        public SigmaGraphPayloadDto Empty(Guid graphId) => new()
        {
            GraphId = graphId,
            Nodes = Array.Empty<SigmaGraphNodeDto>(),
            Edges = Array.Empty<SigmaGraphEdgeDto>()
        };

    }
}
