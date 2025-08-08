using System.Net;

namespace Events.Core.Dtos
{
    public record ScrapePageResultDto
    {
        //Meta data:
        public required Uri OriginalUrl { get; init; }
        public required Uri Url { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public bool IsRedirect { get; init; }
        public DateTimeOffset? SourceLastModified { get; init; }

        //Blob data:
        public string? BlobId { get; init; }
        public string? BlobContainer { get; init; }
        public string? ContentType { get; init; }
        public string? Encoding { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
