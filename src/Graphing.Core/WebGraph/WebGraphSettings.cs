
namespace Graphing.Core.WebGraph
{
    public class WebGraphSettings
    {
        public int ScheduleCrawlThrottleSeconds { get; set; } = 60;
        public NodeEdgesUpdateMode NodeEdgesUpdateMode { get; set; } = NodeEdgesUpdateMode.Append;
    }
}
