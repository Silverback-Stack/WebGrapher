using System;
using System.Net;

namespace Events.Core.EventTypes
{
    public record NormalisePageEvent
    {
        public required CrawlPageEvent CrawlPageEvent { get; init; }
        public required Uri OriginalUrl { get; init; }
        public required Uri Url { get; init; }
        public bool IsRedirect { get; init; }

        public string? BlobId { get; init; }
        public string? BlobContainer { get; init; } 
        public string? ContentType { get; init; }
        public string? Encoding { get; init; }
        public HttpStatusCode StatusCode { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? LastModified { get; init; }
    }
}
