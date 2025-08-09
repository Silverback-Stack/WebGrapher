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

        //Priority queues per event type
        private readonly ConcurrentDictionary<Type, SortedDictionary<int, ConcurrentQueue<(object Event, DateTimeOffset? Scheduled)>>> _eventQueues = new();

        public InMemoryEventBusAdapter(ILogger logger) : base(logger) { }

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
        public override void Dispose()
        {
            _logger.LogDebug($"Disposing event bus {typeof(InMemoryEventBusAdapter).Name}, handlers cleared.");
            _handlers.Clear();
        }

        public override void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            _handlers.AddOrUpdate(typeof(TEvent),
                _ => new List<Delegate> { handler },
                (_, list) => { list.Add(handler); return list; }
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

        public override Task PublishAsync<TEvent>(
            TEvent @event,
            int priority = 0,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default) where TEvent : class
        {
            // In-memory bus doesnt support scheduled events so we simulate it using Task.Delay
            // To prevent blocking main thread, we can
            // Fire and forget the whole dispatch on a background thread
            // We must remove async from the method signature , but for other implementations it will be more relevent

            _ = Task.Run(async () => //background thread
            {
                var delay = scheduledEnqueueTime.HasValue
                    ? scheduledEnqueueTime.Value - DateTimeOffset.UtcNow
                    : TimeSpan.Zero;

                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning($"Timout for event {typeof(TEvent).Name}, delay cancelled after {delay.TotalSeconds} seconds.");
                        return;
                    }
                }

                if (_handlers.TryGetValue(typeof(TEvent), out var subscribers))
                {
                    foreach (var handler in subscribers.Cast<Func<TEvent, Task>>())
                    {
                        try
                        {
                            _ = handler(@event); // fire-and-forget, or add await for sequential

                            var scheduled = delay.TotalSeconds > 0 ? $"scheduled in {delay.TotalSeconds} seconds." : string.Empty;

                            _logger.LogDebug($"Handler executed for event {typeof(TEvent).Name} Priority: {priority} {scheduled}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Handler execution failed for event {EventType}", typeof(TEvent).Name);
                        }
                    }
                }
            });

            // Return immediately without waiting for handlers
            return Task.CompletedTask;
        }

    }
}
