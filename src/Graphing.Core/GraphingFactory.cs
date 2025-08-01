using System;
using Events.Core.Bus;
using Graphing.Core.Adapters.InMemory;
using Microsoft.Extensions.Logging;

namespace Graphing.Core
{
    public class GraphingFactory
    {
        public static IGraph Create(ILogger logger, IEventBus eventBus)
        {
            var service = new InMemoryGraphAdapter(logger, eventBus);
            service.SubscribeAll();
            return service;
        }
    }
}
