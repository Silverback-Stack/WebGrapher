
namespace Graphing.Core.WebGraph.Models
{
    public class Graph
    {
        public Guid Id { get; init; }
        public required string UserId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public required GraphCrawlRequest CrawlRequest { get; set; }
    }
}
