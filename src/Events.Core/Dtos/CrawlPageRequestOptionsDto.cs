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
        public string UrlMatchRegex { get; init; }
        public string TitleElementXPath { get; init; }
        public string ContentElementXPath { get; init; }
        public string SummaryElementXPath { get; init; }
        public string ImageElementXPath { get; init; }
        public string RelatedLinksElementXPath { get; init; }
    }
}
