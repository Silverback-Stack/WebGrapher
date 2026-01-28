using Events.Core.Bus;
using Graphing.Core;
using Graphing.Core.WebGraph;
using Graphing.Infrastructure.WebGraph.Adapters.AzureCosmosGremlin;
using Graphing.Infrastructure.WebGraph.Adapters.Memory;
using Graphing.Infrastructure.WebGraph.Serializers.SigmaJs;
using Microsoft.Extensions.Logging;
using System;

namespace Graphing.Factories
{
    public class GraphingFactory
    {
        public static IPageGrapher Create(ILogger logger, IEventBus eventBus, GraphingConfig graphingConfig)
        {
            //Create WebGraph
            IWebGraph webGraph;
            switch (graphingConfig.Provider)
            {
                case GraphingProvider.Memory:
                    webGraph = new MemoryWebGraphAdapter(logger, graphingConfig.Settings);
                    break;

                case GraphingProvider.AzureCosmosGremlin:
                    webGraph = new AzureCosmosGremlinWebGraphAdapter(logger, graphingConfig.Settings, graphingConfig.AzureCosmosGremlin);
                    break;

                default:
                    throw new NotSupportedException($"{graphingConfig.Provider} is not supported.");
            }

            //Create Payload Serializer
            IWebGraphPayloadSerializer payloadSerializer = new SigmaJsGraphPayloadSerializer(graphingConfig.SigmaJsGraph);

            var service = new PageGrapher(
                logger, 
                eventBus, 
                webGraph, 
                graphingConfig.Settings,
                payloadSerializer);

            return service;
        }
    }
}
