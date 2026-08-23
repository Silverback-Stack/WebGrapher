namespace Events.Core.Dtos
{
    public record CrawlPageRequestDto
    {
        public required Uri Url { get; init; }
        public Guid GraphId { get; init; }
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public int Attempt { get; init; } = 1;
        public int Depth { get; init; } = 0;
        public bool Preview { get; init; } = false;
        public required CrawlPageRequestOptionsDto Options { get; init; }
        public DateTimeOffset RequestedAt { get; init; }

    }
}
