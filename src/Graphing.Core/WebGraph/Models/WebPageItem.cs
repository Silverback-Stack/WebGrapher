namespace Graphing.Core.WebGraph.Models
{
    public class WebPageItem
    {
        public Guid GraphId { get; set; }
        public string Url { get; set; }
        public string OriginalUrl { get; set; }
        public bool IsRedirect { get; set; }
        public DateTimeOffset? SourceLastModified { get; set; }

        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? ImageUrl { get; set; }
        public string? Keywords { get; set; }
        public IEnumerable<string>? Tags { get; set; }
        public IEnumerable<string> Links { get; set; }
        public string? DetectedLanguageIso3 { get; set; }
    }
}