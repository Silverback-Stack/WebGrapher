using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Core.Bus
{
    public static class EventBusFactory
    {
        public static IEventBus Create()
        {
            return new MemoryEventBusAdapter();
        }
    }
}
