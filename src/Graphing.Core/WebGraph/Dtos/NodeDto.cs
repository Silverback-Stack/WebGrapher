using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph.Dtos
{
    public record NodeDto
    {
        public Guid GraphId { get; init; }
        public string Url { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public IEnumerable<string> Tags { get; init; } = Enumerable.Empty<string>();
        public HashSet<Node> OutgoingLinks { get; init; } = new();
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? SourceLastModified { get; set; }
    }
}
