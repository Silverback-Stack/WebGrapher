using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logging.Core;

namespace Events.Core.Bus
{
    public static class EventBusFactory
    {
        public static IEventBus CreateEventBus(IAppLogger appLogger)
        {
            return new MemoryEventBusAdapter(appLogger);
        }
    }
}
