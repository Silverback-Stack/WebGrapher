using System;

namespace Requests.Core
{
    public interface IRequestSender
    {
        Task<HttpResponseEnvelope?> FetchAsync(
            Uri url,
            string userAgent, 
            string userAccepts, 
            int contentMaxBytes = 0,
            string compositeKey = "",
            CancellationToken cancellationToken = default);
    }
}
