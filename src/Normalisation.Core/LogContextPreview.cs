using System;

namespace Normalisation.Core
{
    public record LogContextPreview
    {
        public string? Title { get; init; }
        public string? Summary { get; init; }
        public string? Keywords { get; init; }
        public IEnumerable<string>? Tags { get; init; }
        public IEnumerable<string>? Links { get; init; }
        public string? ImageUrl { get; init; }
        public string? DetectedLanguageIso3 { get; init; }
    }
}
