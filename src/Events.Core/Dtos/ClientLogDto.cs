using System;
using Events.Core.Events.LogEvents;

namespace Events.Core.Dtos
{
    public record ClientLogDto
    {
        public required Guid Id { get; init; }
        public required Guid GraphId { get; init; }
        public required Guid CorrelationId { get; set; }
        public String Type { get; init; } = string.Empty;
        public required string Message { get; init; } = string.Empty;
        public string? Code { get; init; }
        public required string Service { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public Object? Context { get; init; }
    }
}
