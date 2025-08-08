namespace Events.Core.Dtos
{
    public record SigmaNodeDto
    {
        public required string Id { get; init; }
        public string Label { get; init; } = string.Empty;
        public double Size { get; init; }
        public string Color { get; init; } = string.Empty;
    }
}
