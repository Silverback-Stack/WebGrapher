namespace Graphing.Core.WebGraph.Models
{
    public class WebPageItem
    {
        public int GraphId { get; set; }
        public string Url { get; set; }
        public string OriginalUrl { get; set; }
        public bool IsRedirect { get; set; }
        public DateTimeOffset? SourceLastModified { get; set; }

        public string? Title { get; init; }
        public string? Keywords { get; init; }
        public IEnumerable<string>? Tags { get; init; }
        public IEnumerable<string> Links { get; set; }
        public string? DetectedLanguageIso3 { get; set; }
    }
}