using System;
using Events.Core.Dtos;

namespace Events.Core.EventTypes
{
    public record GraphNodeAddedEvent
    {
        public required int GraphId { get; set; }
        public required IEnumerable<SigmaGraphNodeDto> Nodes { get; set; }
        public required IEnumerable<SigmaGraphEdgeDto> Edges { get; set; }
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
    }
}
