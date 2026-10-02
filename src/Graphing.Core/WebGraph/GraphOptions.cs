namespace Graphing.Core.WebGraph
{
    /// <summary>
    /// Defines the options used to create a WebGraph, including the crawl
    /// configuration persisted with the Graph for subsequent crawls.
    /// </summary>
    public record GraphOptions
    {
        // Fallback when no client browser User-Agent is provided
        public const string DefaultUserAgent = "WebGrapher/1.0";

        // Crawling currently supports HTML and plain text content only
        public const string DefaultUserAccepts = "text/html,text/plain";


        // Graph options
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;


        // Crawl options persisted with the Graph
        public Uri? Url { get; set; }
        public int MaxLinks { get; set; } = 1;
        public int MaxDepth { get; set; } = 1;
        public bool ExcludeExternalLinks { get; set; } = true;
        public bool ExcludeQueryStrings { get; set; } = true;
        public bool ConsolidateQueryStrings { get; set; } = true;
        public string UrlMatchRegex { get; set; } = string.Empty;
        public string TitleElementXPath { get; set; } = string.Empty;
        public string ContentElementXPath { get; set; } = string.Empty;
        public string SummaryElementXPath { get; set; } = string.Empty;
        public string ImageElementXPath { get; set; } = string.Empty;
        public string RelatedLinksElementXPath { get; set; } = string.Empty;
        public string UserAgent { get; set; } = DefaultUserAgent;
        public string UserAccepts { get; set; } = DefaultUserAccepts;
    }
}
