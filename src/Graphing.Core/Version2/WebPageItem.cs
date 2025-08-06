namespace Graphing.Core.Version2
{
    public class WebPageItem
    {
        public string Url { get; set; }
        public int GraphId { get; set; }
        public string OriginalUrl { get; set; }
        public IEnumerable<string> Links { get; set; }
        public bool IsRedirect { get; set; }
        public DateTimeOffset? SourceLastModified { get; set; }
    }
}