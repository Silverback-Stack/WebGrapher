
namespace Graphing.Core.WebGraph.Models
{
    public record Graph
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public string Url { get; set; } = string.Empty;
        public int MaxDepth { get; init; } = 3;
        public int MaxLinks { get; init; } = 10;
        public bool FollowExternalLinks { get; init; } = false;
        public bool ExcludeQueryStrings { get; init; } = true;
        public string UrlMatchRegex { get; init; } = string.Empty;
        public string TitleElementXPath { get; init; } = string.Empty;
        public string ContentElementXPath { get; init; } = string.Empty;
        public string SummaryElementXPath { get; init; } = string.Empty;
        public string ImageElementXPath { get; init; } = string.Empty;
        public string RelatedLinksElementXPath { get; init; } = string.Empty;
    }
}
