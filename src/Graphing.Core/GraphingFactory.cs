using System;
using Events.Core.Bus;
using Graphing.Core.WebGraph;
using Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin;
using Graphing.Core.WebGraph.Adapters.Memory;
using Microsoft.Extensions.Logging;

namespace Graphing.Core
{
    public class GraphingFactory
    {
        public static IPageGrapher Create(ILogger logger, IEventBus eventBus, GraphingSettings graphingSettings)
        {
            //Create WebGraph
            IWebGraph webGraph;
            switch (graphingSettings.WebGraph.Provider)
            {
                case WebGraphProvider.Memory:
                    webGraph = new MemoryWebGraphAdapter(logger, graphingSettings);
                    break;

                case WebGraphProvider.AzureCosmosGremlin:
                    webGraph = new AzureCosmosGremlinWebGraphAdapter(logger, graphingSettings);
                    break;

                default:
                    throw new NotSupportedException($"{graphingSettings.WebGraph.Provider} is not supported.");
            }

            var service = new PageGrapher(logger, eventBus, webGraph, graphingSettings);
            service.SubscribeAll();
            return service;
        }
    }
}
