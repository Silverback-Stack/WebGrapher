using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Logging.Core;

namespace Events.Core.Bus
{
    public abstract class BaseEventBus : IEventBus
    {
        internal readonly ILogger _logger = LoggingFactory.Create(LoggingOptions.File, nameof(BaseEventBus));

        
        public abstract void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        public abstract Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
        public abstract Task StartAsync();
        public abstract Task StopAsync();
        public abstract void Dispose();

    }
}
