using System;
using Logging.Core;

namespace Events.Core.Bus
{
    public abstract class BaseEventBus : IEventBus
    {
        internal readonly ILogger _logger;

        public BaseEventBus(ILogger logger)
        {
            _logger = logger;
        }

        public abstract void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        public abstract void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        public abstract Task PublishAsync<TEvent>(
            TEvent @event,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default) where TEvent : class;

        public abstract Task StartAsync();
        public abstract Task StopAsync();
        public abstract void Dispose();
    }
}
