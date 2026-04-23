using System.Net;

namespace Requests.Core
{
    public record HttpResponseMetadata
    {
        // The URL that was originally requested
        public required Uri OriginalUrl { get; set; }

        // The final resolved URL after any redirects
        public required Uri Url { get; set; }

        public HttpStatusCode StatusCode { get; init; }

        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? Expires { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }

        public string? ContentType { get; init; }
        public string? Encoding { get; init; }

        // True if the response includes a cross-origin access policy header
        public bool HasCorsPolicy { get; set; }

        // True if the final URL differs from the originally requested URL
        public bool IsRedirect => OriginalUrl != Url;
    }
}


