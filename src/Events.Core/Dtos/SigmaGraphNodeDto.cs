using System.Text.Json.Serialization;

namespace Events.Core.Dtos
{
    public record SigmaGraphNodeDto
    {
        // Standard SigmaJs properties (must start lower case)
        [JsonPropertyName("id")]
        public required string Id { get; init; } //Url

        [JsonPropertyName("label")]
        public string Label { get; init; } = string.Empty; //Title

        [JsonPropertyName("popularityScore")]
        public double PopularityScore { get; init; } = 0;

        [JsonPropertyName("size")]
        public double Size { get; init; } = 1;


        // Extra properties for SigmaJs (must start lower case)
        [JsonPropertyName("state")]
        public required string State { get; set; }

        [JsonPropertyName("summary")]
        public required string Summary { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("type")]
        public string? Type => Image != null ? "image" : "circle";

        [JsonPropertyName("tags")]
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();

        [JsonPropertyName("sourceLastModified")]
        public DateTimeOffset? SourceLastModified { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; init; }

    }
}
