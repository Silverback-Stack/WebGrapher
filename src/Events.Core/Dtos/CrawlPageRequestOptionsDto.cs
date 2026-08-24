namespace Events.Core.Dtos
{
    public record CrawlPageRequestOptionsDto
    {
        public required string UserAgent { get; init; }
        public required string UserAccepts { get; init; }

        public int MaxDepth { get; init; }
        public int MaxLinks { get; init; }
        public bool ExcludeExternalLinks { get; init; }
        public bool ExcludeQueryStrings { get; init; }
        public bool ConsolidateQueryStrings { get; init; }
        public string UrlMatchRegex { get; init; } = string.Empty;
        public string TitleElementXPath { get; init; } = string.Empty;
        public string ContentElementXPath { get; init; } = string.Empty;
        public string SummaryElementXPath { get; init; } = string.Empty;
        public string ImageElementXPath { get; init; } = string.Empty;
        public string RelatedLinksElementXPath { get; init; } = string.Empty;
    }
}
