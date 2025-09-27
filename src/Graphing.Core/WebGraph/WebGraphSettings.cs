
using Graphing.Core.WebGraph.Adapters.AzureCosmosGremlin;
using Graphing.Core.WebGraph.Adapters.SigmaJs;

namespace Graphing.Core.WebGraph
{
    public class WebGraphSettings
    {
        public WebGraphType WebGraphType { get; set; } = WebGraphType.Memory;
        public int ScheduleCrawlThrottleSeconds { get; set; } = 60;
        public NodeEdgesUpdateMode NodeEdgesUpdateMode { get; set; } = NodeEdgesUpdateMode.Append;

        public SigmaJsGraphSettings SigmaJsGraph { get; set; } = new SigmaJsGraphSettings();

        public AzureCosmosGremlinSettings AzureCosmosGremlin { get; set; } = new AzureCosmosGremlinSettings();
    }
}
