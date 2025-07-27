using System;
using System.Net;

namespace ScraperService
{
    public record ScrapeResponseItem
    {
        public required Uri Url { get; init; }
        public required string Content { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
        public bool IsFromCache { get; init; }
    }
}
