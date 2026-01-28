using System;

namespace Events.Core.Dtos
{

    // SigmaGraph* DTOs are the contract consumed by the web client (Sigma.js).
    // They are intentionally Sigma-shaped and not intended as a generic graph representation.

    public record SigmaGraphPayloadDto
    {
        public required Guid GraphId { get; set; }
        public Guid? CorrolationId { get; set; } = null;
        public required IEnumerable<SigmaGraphNodeDto> Nodes { get; set; }
        public required IEnumerable<SigmaGraphEdgeDto> Edges { get; set; }

        // Metrics
        public int NodeCount => Nodes.Count();
        public int EdgeCount => Edges.Count();

        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    }
}
