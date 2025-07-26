using System.Net;

namespace Requests.Core
{
    public record RequestResponseItem
    {
        public required Uri OriginalUrl {  get; set; }
        public Uri? RedirectedUrl { get; set; }
        public string? Content { get; init; }
        public string? ContentType { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? Expires { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
    }

}
