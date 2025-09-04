using System;
using Events.Core.Bus;
using Graphing.Core.WebGraph;
using Microsoft.Extensions.Logging;

namespace Graphing.Core
{
    public class GraphingFactory
    {
        public static IPageGrapher Create(ILogger logger, IEventBus eventBus, IWebGraph webGraph, GraphingSettings graphingSettings)
        {
            var service = new PageGrapher(logger, eventBus, webGraph, graphingSettings);
            service.SubscribeAll();
            return service;
        }
    }
}
