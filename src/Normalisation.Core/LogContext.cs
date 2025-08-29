using System;

namespace Normalisation.Core
{
    public record LogContext
    {
        public required string Url { get; init; }
        public string Title { get; init; } = string.Empty;
        public int TotalLinks { get; init; }
        public int TotalKeywords { get; init; }
    }
}
