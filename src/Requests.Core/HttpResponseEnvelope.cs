
namespace Requests.Core
{
    public record HttpResponseEnvelope
    {
        public required HttpResponseMetadata Metadata { get; set; }
        public HttpResponseData? Data { get; set; }
        public bool IsFromCache { get; set; }
    }

}


