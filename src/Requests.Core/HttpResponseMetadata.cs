using System.Net;

namespace Requests.Core
{
    public record HttpResponseMetadata
    {
        public required Uri OriginalUrl { get; set; }
        public required Uri Url { get; set; }
        public HttpStatusCode StatusCode { get; init; }
        public DateTimeOffset? LastModified { get; init; }
        public DateTimeOffset? Expires { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
        public HttpResponseDataItem? ResponseData { get; set; }

        public bool IsRedirect => Url is not null &&
            OriginalUrl == Url ? false : true;
    }

}


