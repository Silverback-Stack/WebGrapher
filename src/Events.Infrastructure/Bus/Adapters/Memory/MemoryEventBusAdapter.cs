using System;
using System.Collections.Concurrent;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

namespace Events.Infrastructure.Bus.Adapters.Memory
{
    public class MemoryEventBusAdapter : BaseEventBus
    {
        //Continer for subscribers/handlers
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

        // Semaphore per event type for rate limiting
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> _semaphores = new();

        // Concurrency limit per event type
        private readonly int _maxConcurrencyLimitPerEvent;

        public MemoryEventBusAdapter(
            ILogger logger,
            int maxConcurrencyLimitPerEvent) : base(logger)
        {
            _maxConcurrencyLimitPerEvent = maxConcurrencyLimitPerEvent;
        }

        public async override Task StartAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogDebug($"Started event bus {typeof(MemoryEventBusAdapter).Name}");
        }

        public async override Task StopAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogDebug($"Stopped event bus {typeof(MemoryEventBusAdapter).Name}");
        }

        public override async Task Subscribe<TEvent>(string serviceName, Func<TEvent, Task> handler) where TEvent : class
        {
            // Ensure a concurrency semaphore exists for this event type.
            // In-memory adapter controls parallel handler execution locally.
            var semaphore = _semaphores.GetOrAdd(typeof(TEvent), _ 
                => new SemaphoreSlim(_maxConcurrencyLimitPerEvent));

            // ------------------------------------------------------------
            // Event Dispatch Flow (In-Memory Adapter)
            // ------------------------------------------------------------
            // 1. Event is published in-process.
            // 2. Dispatch to subscribed handler.
            // 3. Concurrency is limited via SemaphoreSlim.
            // 4. On success  -> no settlement required (no broker).
            // 5. On failure  -> error is logged only.
            //
            // No retries.
            // No dead-letter queue.
            // No durability.
            //
            // This adapter is intended for local development and
            // architectural validation — not production guarantees.
            // ------------------------------------------------------------

            // Wrap the handler with concurrency control

            Func<TEvent, Task> wrapped = async evt =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await handler(evt);
                }
                finally
                {
                    semaphore.Release();
                }
            };

            // Register the wrapped handler
            _handlers.AddOrUpdate(typeof(TEvent),
                _ => new List<Delegate> { wrapped },
                (_, list) =>
                {
                    list.Add(wrapped);
                    return list;
                });

            _logger.LogDebug($"{serviceName} service subscribed to event {typeof(TEvent).Name}");
        }

        public override async Task Unsubscribe<TEvent>(string serviceName, Func<TEvent, Task> handler) where TEvent : class
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var subscribers))
            {
                subscribers.RemoveAll(h => h.Equals(handler));

                if (subscribers.Count == 0)
                    _handlers.TryRemove(typeof(TEvent), out _);

                _logger.LogDebug($"{serviceName} service unsubscribed from event {typeof(TEvent).Name}");
            }
        }

        public override async Task PublishAsync<TEvent>(
            TEvent @event,
            int priority = 0,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default) where TEvent : class
        {
            // Priority not supported for in-memory adapter
            if (priority != 0)
                _logger.LogDebug("[InMemory] Priority {Priority} requested but not supported; ignoring.", priority);

            // Create scheduled delay if provided
            var delay = scheduledEnqueueTime.HasValue
                    ? scheduledEnqueueTime.Value - DateTimeOffset.UtcNow
                    : TimeSpan.Zero;

            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            // Use a background thread to prevent main thread being blocked
            _ = Task.Run(async () =>
            {
                try
                {
                    // Scheduled event not supported -> simulate it using Task.Delay
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, cancellationToken);

                    await PublishInternalAsync(@event, priority, delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing event.");
                }
            });

            return;
        }

        private async Task PublishInternalAsync<TEvent>(
            TEvent @event,
            int priority,
            TimeSpan delay,
            CancellationToken cancellationToken = default) where TEvent : class
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var subscribers))
            {
                foreach (var handler in subscribers.Cast<Func<TEvent, Task>>())
                {
                    try
                    {
                        await handler(@event);
                        await HandleSuccessAsync(typeof(TEvent), cancellationToken);

                        var scheduled = delay.TotalSeconds > 0 ? $"scheduled in {delay.TotalSeconds} seconds." : string.Empty;

                        _logger.LogDebug($"Handler executed for event {typeof(TEvent).Name} Priority: {priority} {scheduled}");
                    }
                    catch (Exception ex)
                    {
                        await HandleFailureAsync(typeof(TEvent), ex, cancellationToken);
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var semaphore in _semaphores.Values)
                semaphore.Dispose();

            _semaphores.Clear();
            _handlers.Clear();

            _logger.LogDebug($"Disposing event bus {typeof(MemoryEventBusAdapter).Name}, handlers cleared.");
        }


        private Task HandleSuccessAsync(Type eventType, CancellationToken ct)
        {
            // In-memory: nothing to settle/acknowledge
            return Task.CompletedTask;
        }

        private Task HandleFailureAsync(Type eventType, Exception ex, CancellationToken ct)
        {
            // In-memory: no retries/DLQ. We only log.
            _logger.LogError(ex,
                "[InMemory] Handler failed for {EventType}. Retries/DLQ not supported in memory adapter.",
                eventType.Name);

            return Task.CompletedTask;
        }
    }
}
