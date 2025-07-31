using System;
using System.Net;

namespace Events.Core.EventTypes
{
    public record NormalisePageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public required Uri Url { get; init; }
        public string? Title { get; init; }
        public string? Keywords { get; init; }

        public IEnumerable<string> Links { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
