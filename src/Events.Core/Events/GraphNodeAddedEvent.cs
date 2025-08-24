using System;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record GraphNodeAddedEvent
    {
        public required SigmaGraphPayloadDto SigmaGraphPayload { get; set; }
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
    }
}
