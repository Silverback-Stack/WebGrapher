using System;

namespace Graphing.Core.WebGraph.Dtos
{
    public class GraphDto
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

        public required CrawlRequestDto CrawlRequest { get; init; }
    }
}
