using System.Text.Json.Serialization;

namespace Events.Core.Dtos
{
    public record SigmaGraphEdgeDto
    {
        // Standard SigmaJS properties (must start lower case)
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("source")]
        public required string Source { get; set; }

        [JsonPropertyName("target")]
        public required string Target { get; set; }
    }
}
