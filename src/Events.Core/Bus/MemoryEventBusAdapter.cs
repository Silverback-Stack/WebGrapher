using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Events.Core.Bus
{
    public class MemoryEventBusAdapter : BaseEventBus
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

        public override void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            _handlers.AddOrUpdate(
                typeof(TEvent),
                _ => new List<Delegate> { handler },
                (_, list) => { list.Add(handler); return list; }
            );
            _logger.LogInformation($"Subscribed: {typeof(TEvent).Name}");
        }

        public override Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var subscribers))
            {
                foreach (var handler in subscribers.Cast<Func<TEvent, Task>>())
                {
                    handler(@event); // fire and forget (can be awaited if needed)
                    _logger.LogInformation($"Published: {typeof(TEvent).Name}");
                }
            }
            return Task.CompletedTask;
        }

        public async override Task StartAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogInformation($"Started: {typeof(MemoryEventBusAdapter).Name}");
        }

        public async override Task StopAsync()
        {
            //nothing to do for in-memory implementation
            _logger.LogInformation($"Stopped: {typeof(MemoryEventBusAdapter).Name}");
        }

        public override void Dispose()
        {
            _handlers.Clear();
            _logger.LogInformation($"Disposed: {typeof(MemoryEventBusAdapter).Name} and handlers cleared.");
        }

    }
}
