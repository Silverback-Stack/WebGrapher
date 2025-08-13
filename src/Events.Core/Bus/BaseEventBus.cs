using System;
using System.Collections.Concurrent;
using Events.Core.RateLimiters;
using Microsoft.Extensions.Logging;

namespace Events.Core.Bus
{
    public abstract class BaseEventBus : IEventBus
    {
        internal readonly ILogger _logger;

        private readonly ConcurrentDictionary<Type, IEventRateLimiter> _rateLimiters = new();

        public BaseEventBus(ILogger logger, Dictionary<Type, int>? concurrencyLimits = null)
        {
            _logger = logger;

            if (concurrencyLimits != null)
            {
                foreach (var kvp in concurrencyLimits)
                {
                    _rateLimiters[kvp.Key] = new SemaphoreEventRateLimiter(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Wraps a subscriber in rate limiter logic if a limit is configured for the event type.
        /// </summary>
        protected Func<TEvent, Task> WrapWithLimiter<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            if (_rateLimiters.TryGetValue(typeof(TEvent), out var limiter))
            {
                return async (evt) =>
                {
                    await limiter.WaitAsync();
                    try
                    {
                        await handler(evt);
                    }
                    finally
                    {
                        limiter.Release();
                    }
                };
            }

            return handler;
        }

        public abstract void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        public abstract void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        public abstract Task PublishAsync<TEvent>(
            TEvent @event,
            int priority = 0,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default) where TEvent : class;

        public abstract Task StartAsync();
        public abstract Task StopAsync();
        public virtual void Dispose()
        {
            foreach (var limiter in _rateLimiters.Values)
                limiter.Dispose();
        }
    }
}
