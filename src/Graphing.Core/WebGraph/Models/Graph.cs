
namespace Graphing.Core.WebGraph.Models
{
    public record Graph
    {
        public Guid Id { get; init; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public string Url { get; set; } = string.Empty;
        public int MaxDepth { get; set; } = 3;
        public int MaxLinks { get; set; } = 10;
        public bool ExcludeExternalLinks { get; set; } = true;
        public bool ExcludeQueryStrings { get; set; } = true;
        public string UrlMatchRegex { get; set; } = string.Empty;
        public string TitleElementXPath { get; set; } = string.Empty;
        public string ContentElementXPath { get; set; } = string.Empty;
        public string SummaryElementXPath { get; set; } = string.Empty;
        public string ImageElementXPath { get; set; } = string.Empty;
        public string RelatedLinksElementXPath { get; set; } = string.Empty;
        public string UserAgent {  get; set; } = string.Empty;
        public string UserAccepts { get; set; } = string.Empty;
    }
}
