using Graphing.Core;
using Graphing.Infrastructure.WebGraph.Adapters.AzureCosmosGremlin;
using Graphing.Infrastructure.WebGraph.Serializers.SigmaJs;
using System;

namespace Graphing.Factories
{
    public class GraphingConfig
    {
        public GraphingSettings Settings { get; set; } = new GraphingSettings();

        public GraphingProvider Provider { get; set; } = GraphingProvider.Memory;

        public AzureCosmosGremlinSettings AzureCosmosGremlin { get; set; } = new AzureCosmosGremlinSettings();

        public SigmaJsGraphSettings SigmaJsGraph { get; set; } = new SigmaJsGraphSettings();
    }
}
