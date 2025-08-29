using System;

namespace Crawler.Core
{
    public record LogContext
    {
        public required string Url { get; init; }
        public int Depth { get; init; }
        public int Attempt { get; init; }
    }
}
