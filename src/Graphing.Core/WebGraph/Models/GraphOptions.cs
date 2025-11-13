
namespace Graphing.Core.WebGraph.Models
{
    /// <summary>
    /// Encapsulates configuration options for graph operations, 
    /// allowing methods to receive a single object instead of multiple parameters.
    /// </summary>
    public record GraphOptions
    {
        public const string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        public const string DEFAULT_USER_ACCEPTS = "text/html,text/plain";

        public Guid? Id { get; set; } = null;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Uri? Url { get; set; } = null;
        public int MaxLinks { get; set; } = 1;
        public int MaxDepth { get; set; } = 1;
        public bool ExcludeExternalLinks { get; set; } = true;
        public bool ExcludeQueryStrings { get; set; } = true;
        public string UrlMatchRegex { get; set; } = string.Empty;
        public string TitleElementXPath { get; set; } = string.Empty;
        public string ContentElementXPath { get; set; } = string.Empty;
        public string SummaryElementXPath { get; set; } = string.Empty;
        public string ImageElementXPath { get; set; } = string.Empty;
        public string RelatedLinksElementXPath { get; set; } = string.Empty;
        public string UserAgent { get; set; } = DEFAULT_USER_AGENT;
        public string UserAccepts { get; set; } = DEFAULT_USER_ACCEPTS;
        public bool Preview { get; init; } = false;
    }
}
