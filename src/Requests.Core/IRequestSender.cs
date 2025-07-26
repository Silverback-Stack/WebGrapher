using System;

namespace Requests.Core
{
    public interface IRequestSender
    {
        Task<RequestResponseItem?> GetStringAsync(Uri url, CancellationToken cancellationToken = default);
        Task<RequestResponseItem?> GetStringAsync(
            Uri url, 
            string? userAgent, 
            string? userAccepts, 
            int contentMaxBytes = 0,
            CancellationToken cancellationToken = default);
    }
}
