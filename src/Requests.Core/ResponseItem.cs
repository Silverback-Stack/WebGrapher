using System.Net;

namespace Requests.Core
{
    public record ResponseItem
    {
        public required Uri OriginalUrl { get; set; }
        public Uri? RedirectedUrl { get; set; }
        public string? Content { get; init; }
        public string? ContentType { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? Expires { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }

        /// <summary>
        /// The Url of the final destination after any redirects.
        /// </summary>
        public Uri Url => RedirectedUrl is not null && 
            RedirectedUrl != OriginalUrl ? RedirectedUrl : OriginalUrl;

    }

}
