using System;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record ScrapePageEvent
    {
        public required CrawlPageRequestDto CrawlPageRequest { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
