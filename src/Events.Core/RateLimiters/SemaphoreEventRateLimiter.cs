using System;

namespace Events.Core.RateLimiters
{
    public class SemaphoreEventRateLimiter : IEventRateLimiter
    {
        private readonly SemaphoreSlim _semaphore;

        public SemaphoreEventRateLimiter(int maxConcurrency)
        {
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        public Task WaitAsync(CancellationToken cancellationToken = default) =>
            _semaphore.WaitAsync(cancellationToken);

        public void Release() => _semaphore.Release();

        public void Dispose() => _semaphore.Dispose();
    }

}
