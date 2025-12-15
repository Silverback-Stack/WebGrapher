namespace Events.Core.Dtos
{
    public record CrawlPageRequestDto
    {
        public Uri Url { get; init; }
        public Guid GraphId { get; init; }
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public int Attempt { get; init; } = 1;
        public int Depth { get; init; } = 0;
        public bool Preview { get; init; } = false;
        public CrawlPageRequestOptionsDto Options { get; init; }
        public DateTimeOffset RequestedAt { get; init; }

        /// <summary>
        /// Properties that make the network request unique.
        /// </summary>
        public string RequestCompositeKey =>
            string.Join("|", new[]
            {
                Url?.AbsoluteUri ?? string.Empty,
                Options.UserAgent ?? string.Empty,
                Options.UserAccepts ?? string.Empty
            });

        /// <summary>
        /// Properties that make the extracted data unique.
        /// </summary>
        public string FingerprintCompositeKey =>
            string.Join("|", new[]
            {
                        Url?.AbsoluteUri ?? string.Empty,
                        Options.MaxDepth.ToString(),
                        Options.MaxLinks.ToString(),
                        Options.ExcludeExternalLinks.ToString(),
                        Options.ExcludeQueryStrings.ToString(),
                        Options.ConsolidateQueryStrings.ToString(),
                        Options.UrlMatchRegex ?? string.Empty,
                        Options.TitleElementXPath ?? string.Empty,
                        Options.ContentElementXPath ?? string.Empty,
                        Options.SummaryElementXPath ?? string.Empty,
                        Options.ImageElementXPath ?? string.Empty,
                        Options.RelatedLinksElementXPath ?? string.Empty,
                        Options.UserAgent ?? string.Empty,
                        Options.UserAccepts ?? string.Empty
            });

    }
}
