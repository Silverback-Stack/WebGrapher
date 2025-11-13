using System;
using System.Text.Json.Serialization;

namespace Normalisation.Core
{
    /// <summary>
    /// This type is assigned to an Object property, so all members are annotated
    /// to ensure consistent JSON serialization across .NET versions (e.g., .NET 8 vs .NET 9).
    /// </summary>
    public record LogContext
    {
        [JsonPropertyName("url")]
        public required string Url { get; init; }

        [JsonPropertyName("totalLinks")]
        public int TotalLinks { get; set; }

        [JsonPropertyName("totalKeywords")]
        public int TotalKeywords { get; set; }

        [JsonPropertyName("preview")]
        public LogContextPreview? Preview { get; set; }
    }
}
