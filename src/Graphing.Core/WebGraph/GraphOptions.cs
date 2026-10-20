using Graphing.Core.WebGraph.Models;

namespace Graphing.Core.WebGraph
{
    /// <summary>
    /// Defines the parameters for graph operations, including
    /// the configuration persisted with the graph.
    /// </summary>
    public record GraphOptions
    {
        // Fallback when no client browser User-Agent is provided
        public const string DefaultUserAgent = "WebGrapher/1.0";

        // Crawling currently supports HTML and plain text content only
        public const string DefaultUserAccepts = "text/html,text/plain";

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public required GraphCrawlRequest CrawlRequest { get; set; }

        public string UserAgent { get; set; } = DefaultUserAgent;
        public string UserAccepts { get; set; } = DefaultUserAccepts;
    }
}
