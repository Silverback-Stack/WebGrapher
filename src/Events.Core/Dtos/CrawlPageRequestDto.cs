
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

        public string ToCompositeKey =>
            string.Join("|", new[]
            {
                Url?.AbsoluteUri ?? string.Empty,
                IncludeDepth(Depth),
                MaxDepth.ToString(),
                MaxLinks.ToString(),
                FollowExternalLinks.ToString(),
                ExcludeQueryStrings.ToString(),
                UrlMatchRegex ?? string.Empty,
                TitleElementXPath ?? string.Empty,
                ContentElementXPath ?? string.Empty,
                SummaryElementXPath ?? string.Empty,
                ImageElementXPath ?? string.Empty,
                RelatedLinksElementXPath ?? string.Empty,
                UserAgent ?? string.Empty,
                UserAccepts ?? string.Empty
            });


        /// <summary>
        /// Includes depth in the key only for user requests (depth = 0).
        /// Ensures user-requested pages are cached separately while all robot-discovered pages share the same key.
        /// </summary>
        private string IncludeDepth(int depth)
        {
            return depth == 0 ? depth.ToString() : string.Empty;
        }
    }
}
