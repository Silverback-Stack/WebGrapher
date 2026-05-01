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
        /// Identifies the request sender partition for this failure.
        /// Used to ensure consistent retries and rate-limiting.
        /// </summary>
        public required string PartitionKey { get; init; }
    }
}
