using System;
using Events.Core.Bus.Adapters.InMemory;
using Events.Core.RateLimiters;
using Microsoft.Extensions.Logging;

namespace Events.Core.Bus
{
    public static class EventBusFactory
    {
        public static IEventBus CreateEventBus(ILogger logger, Dictionary<Type, int>? concurrencyLimits = null)
        {
            return new InMemoryEventBusAdapter(logger, concurrencyLimits);
        }
    }
}
