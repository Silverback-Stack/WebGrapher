using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Events.Core.Bus.Adapters.Memory
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

        public override void Subscribe<TEvent>(string serviceName, Func<TEvent, Task> handler) where TEvent : class
        {
            // Ensure semaphore exists for this event type
            var semaphore = _semaphores.GetOrAdd(typeof(TEvent), _ => new SemaphoreSlim(_maxConcurrencyLimitPerEvent));

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

            _handlers.AddOrUpdate(typeof(TEvent),
                _ => new List<Delegate> { wrapped },
                (_, list) =>
                {
                    list.Add(wrapped);
                    return list;
                });

            _logger.LogDebug($"{serviceName} service subscribed to event {typeof(TEvent).Name}");
        }

        public override void Unsubscribe<TEvent>(string serviceName, Func<TEvent, Task> handler) where TEvent : class
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

            // In-memory bus doesnt support priority queues

            // Create scheduled delay if provided
            var delay = scheduledEnqueueTime.HasValue
                    ? scheduledEnqueueTime.Value - DateTimeOffset.UtcNow
                    : TimeSpan.Zero;


            //Use a background thread to prevent main thread being blocked
            _ = Task.Run(async () =>
            {
                try
                {
                    // Scheduled event:
                    // In-memory bus doesnt support scheduled events so we simulate it using Task.Delay
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

                        var scheduled = delay.TotalSeconds > 0 ? $"scheduled in {delay.TotalSeconds} seconds." : string.Empty;

                        _logger.LogDebug($"Handler executed for event {typeof(TEvent).Name} Priority: {priority} {scheduled}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Handler execution failed for event {EventType}", typeof(TEvent).Name);
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
    }
}
