using Events.Core.Dtos;

namespace Streaming.Core.Models
{
    public record WebNode
    {
        public required IEnumerable<SigmaGraphNodeDto> Nodes { get; set; }
        public required IEnumerable<SigmaGraphEdgeDto> Edges { get; set; }
    }
}
