using System;

namespace Requests.Core
{
    public interface IRequestTransformer
    {
        Task<HttpResponseEnvelope> TransformAsync(
            Uri url,
            HttpResponseMessage? httpResponseMessage, 
            string userAccepts,
            string blobId,
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default);
    }
}
