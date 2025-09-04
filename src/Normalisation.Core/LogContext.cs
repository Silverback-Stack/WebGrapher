using System;

namespace Normalisation.Core
{
    public record LogContext
    {
        public required string Url { get; init; }
        public int TotalLinks { get; set; }
        public int TotalKeywords { get; set; }

        public LogContextPreview? Preview { get; set; }
    }
}
