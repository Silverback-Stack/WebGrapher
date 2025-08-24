namespace Events.Core.Dtos
{
    public record CrawlPageRequestDto
    {
        public Uri Url { get; init; }
        public Guid GraphId { get; init; }
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public int Attempt { get; init; } = 1;
        public int Depth { get; init; } = 0;
        public CrawlPageRequestOptionsDto Options { get; init; }
        public DateTimeOffset RequestedAt { get; init; }

        public string ToCompositeKey =>
            string.Join("|", new[]
            {
                Url?.AbsoluteUri ?? string.Empty,
                IncludeDepth(Depth),
                Options.MaxDepth.ToString(),
                Options.MaxLinks.ToString(),
                Options.ExcludeExternalLinks.ToString(),
                Options.ExcludeQueryStrings.ToString(),
                Options.UrlMatchRegex ?? string.Empty,
                Options.TitleElementXPath ?? string.Empty,
                Options.ContentElementXPath ?? string.Empty,
                Options.SummaryElementXPath ?? string.Empty,
                Options.ImageElementXPath ?? string.Empty,
                Options.RelatedLinksElementXPath ?? string.Empty,
                Options.UserAgent ?? string.Empty,
                Options.UserAccepts ?? string.Empty
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
