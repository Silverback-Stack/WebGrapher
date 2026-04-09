using System;

namespace Requests.Core
{

    /// <summary>
    /// Executes HTTP requests and returns response data; implemented by infrastructure adapters.
    /// </summary>
    public interface IHttpTransport
    {
        Task<HttpResponseEnvelope?> GetAsync(
            Uri uri,
            string userAgent,
            string userAccepts,
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default);
    }
}
