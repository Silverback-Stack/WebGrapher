using System;

namespace Scraper.Core
{
    public record LogContext
    {
        public required string Url { get; init; }
        public int Attempt { get; init; }
        public string StatusCode { get; init; } = string.Empty;
    }
}
