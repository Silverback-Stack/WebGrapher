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
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

        public InMemoryEventBusAdapter(ILogger logger) : base(logger) { }

        public async override Task StartAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogInformation($"Started: {typeof(InMemoryEventBusAdapter).Name}");
        }

        public async override Task StopAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogInformation($"Stopped: {typeof(InMemoryEventBusAdapter).Name}");
        }
        public override void Dispose()
        {
            _logger.LogDebug($"Disposing: {typeof(InMemoryEventBusAdapter).Name}, handlers cleared.");
            _handlers.Clear();
        }

        public override void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            _handlers.AddOrUpdate(typeof(TEvent),
                _ => new List<Delegate> { handler },
                (_, list) => { list.Add(handler); return list; }
            );
            _logger.LogInformation($"Event subscribed: {typeof(TEvent).Name}");
        }

        public override void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var subscribers))
            {
                subscribers.RemoveAll(h => h.Equals(handler));
                _logger.LogInformation($"Event unsubscribed: {typeof(TEvent).Name}");

                if (subscribers.Count == 0)
                {
                    _handlers.TryRemove(typeof(TEvent), out _);
                }
            }
        }

        public override async Task PublishAsync<TEvent>(
            TEvent @event,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default) where TEvent : class
        {
            var delay = scheduledEnqueueTime.HasValue
                ? scheduledEnqueueTime.Value - DateTimeOffset.UtcNow
                : TimeSpan.Zero;

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    _logger.LogDebug($"Event scheduled: {typeof(TEvent).Name}, will be published after {delay.TotalSeconds} seconds.");
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception)
                {
                    _logger.LogWarning($"Timout: {typeof(TEvent).Name}, delay cancelled after {delay.TotalSeconds} seconds.");
                    return;
                }
            }

            if (_handlers.TryGetValue(typeof(TEvent), out var subscribers))
            {
                foreach (var handler in subscribers.Cast<Func<TEvent, Task>>())
                {
                    _ = handler(@event); // fire-and-forget; can choose to await if you want sequential
                    _logger.LogDebug($"Event published: {typeof(TEvent).Name}");
                }
            }
        }

    }
}
