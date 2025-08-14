
namespace Events.Core.Dtos
{
    public record CrawlPageRequestDto
    {
        public Uri Url { get; init; }
        public int GraphId { get; init; }
        public Guid CorrelationId { get; init; }
        public int Attempt { get; init; } = 1;
        public int Depth { get; init; } = 0;
        public int MaxDepth { get; init; }
        public int MaxLinks { get; init; }
        public bool FollowExternalLinks { get; init; }
        public bool ExcludeQueryStrings { get; init; }
        public string UrlMatchRegex { get; init; }
        public string TitleElementXPath { get; init; }
        public string ContentElementXPath { get; init; }
        public string SummaryElementXPath { get; init; }
        public string ImageElementXPath { get; init; }
        public string RelatedLinksElementXPath { get; init; }
        public string UserAgent { get; init; }
        public string UserAccepts { get; init; }
        public DateTimeOffset RequestedAt { get; init; }
    }
}
