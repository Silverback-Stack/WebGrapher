using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus.Adapters.Memory;
using Logging.Core;

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
