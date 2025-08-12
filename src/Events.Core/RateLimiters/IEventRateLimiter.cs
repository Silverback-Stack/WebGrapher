using System;

namespace Events.Core.RateLimiters
{
    public interface IEventRateLimiter : IDisposable
    {
        Task WaitAsync(CancellationToken cancellationToken = default);
        void Release();
    }
}
