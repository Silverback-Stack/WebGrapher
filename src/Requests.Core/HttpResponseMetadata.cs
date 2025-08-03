using System.Net;

namespace Requests.Core
{
    public record HttpResponseMetadata
    {
        public required Uri OriginalUrl { get; set; }
        public Uri? RedirectedUrl { get; set; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? Expires { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
        public HttpResponseDataItem? ResponseData { get; set; }

        public Uri Url => RedirectedUrl is not null &&
            RedirectedUrl != OriginalUrl ? RedirectedUrl : OriginalUrl;
    }

}


