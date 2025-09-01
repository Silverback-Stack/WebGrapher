using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.Dtos
{
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
