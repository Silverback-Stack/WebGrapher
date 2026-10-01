using System;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record GraphWriteToLogEvent
    {
        public required Guid GraphId { get; init; }
        public required int MaxDepth { get; init; }
        public int? MaxNodes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
