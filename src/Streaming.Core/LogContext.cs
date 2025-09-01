using System;

namespace Streaming.Core
{
    public record LogContext
    {
        public int NodeCount { get; init; }
        public int EdgeCount { get; init; }
        public IEnumerable<string> Nodes { get; init; }
    }
}
