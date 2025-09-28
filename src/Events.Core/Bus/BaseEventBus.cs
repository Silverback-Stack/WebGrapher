using System;
using Microsoft.Extensions.Logging;

namespace Events.Core.Bus
{
    public abstract class BaseEventBus : IEventBus
    {
        internal readonly ILogger _logger;

        public BaseEventBus(ILogger logger)
        {
            _logger = logger;
        }

        public abstract void Subscribe<TEvent>(string serviceName, Func<TEvent, Task> handler) where TEvent : class;

        public abstract void Unsubscribe<TEvent>(string serviceName, Func<TEvent, Task> handler) where TEvent : class;

        public abstract Task PublishAsync<TEvent>(
            TEvent @event,
            int priority = 0,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default) where TEvent : class;

        public abstract Task StartAsync();

        public abstract Task StopAsync();

        public virtual void Dispose()
        {
            //nothing to dispose in the base
        }
    }
}
