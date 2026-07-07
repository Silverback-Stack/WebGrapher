using System;

namespace Requests.Core
{

    /// <summary>
    /// Fetches content from HTTP endpoints, handles caching and response details.
    /// </summary>
    public interface IRequestSender
    {
        string GroupKey { get; }

        Task<HttpResponseEnvelope?> FetchAsync(
            Uri url,
            string userAgent, 
            string userAccepts,
            string compositeKey = "",
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default);
    }
}
