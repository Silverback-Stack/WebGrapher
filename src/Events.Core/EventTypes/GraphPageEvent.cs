using System;
using System.Net;

namespace Events.Core.EventTypes
{
    public record GraphPageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public required Uri RequestUrl { get; init; }
        public required Uri ResolvedUrl { get; init; }
        public string Title { get; init; }
        public string Keywords { get; init; }

        public IEnumerable<Uri> Links { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? SourceLastModified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }

        public bool IsRedirect => RequestUrl != ResolvedUrl;
    }
}
