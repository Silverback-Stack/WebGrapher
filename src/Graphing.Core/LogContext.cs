using System;

namespace Graphing.Core
{
    public record LogContext
    {
        public required string Url { get; init; }
        public int NodeCount { get; init; }
        public int EdgeCount { get; init; }
        public int Depth { get; init; }
        public int Attempt { get; init; }
    }
}
