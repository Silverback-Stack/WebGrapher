using System;
using System.Text.Json.Serialization;

namespace Normalisation.Core
{

    /// <summary>
    /// This type is assigned to an Object property, so all members are annotated
    /// to ensure consistent JSON serialization across .NET versions (e.g., .NET 8 vs .NET 9).
    /// </summary>

    public record LogContextPreview
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("summary")]
        public string? Summary { get; init; }

        [JsonPropertyName("keywords")]
        public string? Keywords { get; init; }

        [JsonPropertyName("tags")]
        public IEnumerable<string>? Tags { get; init; }

        [JsonPropertyName("links")]
        public IEnumerable<string>? Links { get; init; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; init; }

        [JsonPropertyName("detectedLanguageIso3")]
        public string? DetectedLanguageIso3 { get; init; }
    }
}
