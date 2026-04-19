using System;

namespace Requests.Core
{

    /// <summary>
    /// Fetches content from HTTP endpoints, handles caching and response details.
    /// </summary>
    public interface IRequestSender
    {
        string PartitionKey { get; }

        Task<HttpResponseEnvelope?> FetchAsync(
            Uri url,
            string userAgent, 
            string userAccepts, 
            int contentMaxBytes = 0,
            string compositeKey = "",
            CancellationToken cancellationToken = default);
    }
}
