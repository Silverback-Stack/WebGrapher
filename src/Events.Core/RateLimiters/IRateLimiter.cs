using System;

namespace Events.Core.RateLimiters
{
    public interface IRateLimiter : IDisposable
    {
        Task WaitAsync(CancellationToken cancellationToken = default);
        void Release();
    }
}
