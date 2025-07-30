using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Core.Dtos
{
    public record PageEdgeDto
    {
        public string From { get; init; }
        public string To { get; init; }
        public string AnchorText { get; init; }
        public double Weight { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
