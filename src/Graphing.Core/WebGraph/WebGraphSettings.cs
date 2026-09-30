
namespace Graphing.Core.WebGraph
{
    public class WebGraphSettings
    {
        public int NodeRefreshThrottleSeconds { get; set; } = 60;
        public OutgoingNodesUpdateMode OutgoingNodesUpdateMode { get; set; } = OutgoingNodesUpdateMode.Append;
    }
}
