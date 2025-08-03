using System;

namespace Requests.Core
{
    public interface IRequestSender
    {
        Task<HttpResponseEnvelope?> GetStringAsync(
            Uri url, 
            string? userAgent, 
            string? userAccepts, 
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default);

        Task<HttpResponseEnvelope?> GetStringAsync(
            Uri url, 
            CancellationToken cancellationToken = default);
    }
}
