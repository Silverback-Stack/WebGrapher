using System;

namespace Normalisation.Core
{
    public record LogContext
    {
        public required string Url { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string Keywords {  get; init; } = string.Empty;
        public IEnumerable<string> Tags { get; init; } = Enumerable.Empty<string>();
        public IEnumerable<string> Links { get; init; } = Enumerable.Empty<string>();
        public string ImageUrl { get; init; } = string.Empty;
        public string DetectedLanguageIso3 { get; init; } = string.Empty;

        public int TotalLinks => Links.Count();
        public int TotalKeywords => Keywords.Count();
    }
}
