using System;
using System.Net;
using Events.Core.Dtos;

namespace Events.Core.Events
{
    public record ScrapePageFailedEvent
    {
        public required CrawlPageRequestDto CrawlPageRequest { get; init; }

        public HttpStatusCode StatusCode {  get; init; }

        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }

        /// <summary>
        /// Identifies the Request Sender group that handled the request.
        /// </summary>
        public required string RequestSenderGroupKey { get; init; }
    }
}
