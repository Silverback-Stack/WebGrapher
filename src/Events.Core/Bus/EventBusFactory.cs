using System;
using Events.Core.Bus.Adapters.Memory;
using Microsoft.Extensions.Logging;

namespace Events.Core.Bus
{
    public static class EventBusFactory
    {
        public static IEventBus CreateEventBus(ILogger logger)
        {
            return new MemoryEventBusAdapter(logger);
        }
    }
}
