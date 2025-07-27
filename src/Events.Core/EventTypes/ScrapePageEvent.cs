using System;

namespace Events.Core.EventTypes
{
    public record ScrapePageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
