using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Events.Core.Bus.Adapters.InMemory
{
    /// <summary>
    /// In-memory event bus adapter for local development, 
    /// can be swapped out with a distributed event bus adapter such as RabbitMQ or AzureServiceBus.
    /// </summary>
    public class InMemoryEventBusAdapter : BaseEventBus
    {
        //Continer for subscribers/handlers
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

        public InMemoryEventBusAdapter(
            ILogger logger,
            Dictionary<Type, int>? concurrencyLimits = null) : base(logger, concurrencyLimits) { }

        public async override Task StartAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogDebug($"Started event bus {typeof(InMemoryEventBusAdapter).Name}");
        }

        public async override Task StopAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogDebug($"Stopped event bus {typeof(InMemoryEventBusAdapter).Name}");
        }

        public override void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            var wrapped = WrapWithLimiter(handler);

            _handlers.AddOrUpdate(typeof(TEvent),
                _ => new List<Delegate> { wrapped },
                (_, list) => { list.Add(wrapped); return list; }
            );

            _logger.LogDebug($"Subscribed to event {typeof(TEvent).Name}");
        }

        public override void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var subscribers))
            {
                subscribers.RemoveAll(h => h.Equals(handler));
                
                if (subscribers.Count == 0)
                {
                    _handlers.TryRemove(typeof(TEvent), out _);
                }

                _logger.LogDebug($"Unsubscribed from event {typeof(TEvent).Name}. Remaining handlers: {subscribers.Count}");
            }
        }

        public override async Task PublishAsync<TEvent>(
            TEvent @event,
            int priority = 0,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default) where TEvent : class
        {
            // In-memory bus doesnt support priority queues

            var delay = scheduledEnqueueTime.HasValue
                    ? scheduledEnqueueTime.Value - DateTimeOffset.UtcNow
                    : TimeSpan.Zero;


            // Scheduled event:
            // In-memory bus doesnt support scheduled events so we simulate it using Task.Delay
            if (delay > TimeSpan.Zero)
            {
                //Use a background thread to prevent main thread being blocked
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                        await PublishInternalAsync(@event, priority, delay, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error publishing event.");
                    }
                });
            }
            else
            {
                // Non-scheduled event:
                try
                {
                    await PublishInternalAsync(@event, priority, delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing event.");
                }
            }  
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
            _logger.LogDebug($"Disposing event bus {typeof(InMemoryEventBusAdapter).Name}, handlers cleared.");
            _handlers.Clear();
        }
    }
}
