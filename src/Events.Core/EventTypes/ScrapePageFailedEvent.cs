using System;
using System.Net;


namespace Events.Core.EventTypes
{
    public record ScrapePageFailedEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public HttpStatusCode StatusCode {  get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
    }
}
