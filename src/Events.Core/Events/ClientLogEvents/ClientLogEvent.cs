using System;

namespace Events.Core.Events.LogEvents
{
    public record ClientLogEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public required Guid GraphId { get; init; }
        public Guid? CorrelationId { get; set; }
        public LogType Type { get; init; }
        public required string Message { get; init; } = string.Empty;
        public string? Code { get; init; }
        public required string Service { get; init; }
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
        public Object? Context { get; init; }
    }
}
