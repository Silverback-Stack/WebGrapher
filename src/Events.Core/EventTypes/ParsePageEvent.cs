using System;
using System.Net;

namespace Events.Core.EventTypes
{
    public record ParsePageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public required Uri RequestUrl { get; init; }
        public required Uri ResolvedUrl { get; init; }
        public required string Content { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
