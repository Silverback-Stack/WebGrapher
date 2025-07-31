using System;
using Events.Core.Bus;
using Graphing.Core.Adapters.Memory;
using Microsoft.Extensions.Logging;

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
