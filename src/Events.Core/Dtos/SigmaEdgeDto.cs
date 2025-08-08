namespace Events.Core.Dtos
{
    public record SigmaEdgeDto
    {
        public required string Id { get; set; }
        public required string Source { get; set; }
        public required string Target { get; set; }
        public string Color { get; set; } = string.Empty;
    }
}
