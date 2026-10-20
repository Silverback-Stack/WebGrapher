
namespace Graphing.Core.WebGraph
{
    public class WebGraphSettings
    {
        public int NodeRefreshThrottleSeconds { get; set; } = 60;
        public NodeEdgeUpdateStrategy OutgoingNodeEdgeUpdateStrategy { get; set; } = NodeEdgeUpdateStrategy.Append;
    }
}
