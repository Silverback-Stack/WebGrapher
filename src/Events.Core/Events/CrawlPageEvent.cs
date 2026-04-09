using System;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record CrawlPageEvent
    {
        public CrawlPageEvent() { }

        public DateTimeOffset CreatedAt { get; init; }

        public required CrawlPageRequestDto CrawlPageRequest { get; init; }
    }
}
