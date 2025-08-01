using System;
using System.Net;

namespace Scraper.Core
{
    public record ScrapeResponseItem
    {
        public required Uri RequestUrl { get; init; }
        public required Uri ResolvedUrl { get; init; }
        public required string Content { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
        public bool IsFromCache { get; init; }
    }
}
