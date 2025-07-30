using System;
using Events.Core.Bus;
using Graphing.Core.Adapters.Memory;
using Logging.Core;

namespace Graphing.Core
{
    public class GraphingFactory
    {
        public static IGraph Create(ILogger logger, IEventBus eventBus)
        {
            var service = new MemoryGraphAdapter(logger, eventBus);
            service.SubscribeAll();
            return service;
        }
    }
}
