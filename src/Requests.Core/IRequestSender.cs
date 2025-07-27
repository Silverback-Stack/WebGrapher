using System;

namespace Requests.Core
{
    public interface IRequestSender
    {
        Task<ResponseEnvelope<ResponseItem>?> GetStringAsync(
            Uri url, 
            string? userAgent, 
            string? userAccepts, 
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default);

        Task<ResponseEnvelope<ResponseItem>?> GetStringAsync(
            Uri url, 
            CancellationToken cancellationToken = default);
    }
}
