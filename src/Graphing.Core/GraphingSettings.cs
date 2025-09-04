using Graphing.Core.WebGraph;

namespace Graphing.Core
{
    public class GraphingSettings
    {
        public string ServiceName { get; set; } = "Graphing";
        public int MaxRequestDepthLimit = 3;
        public int MaxRequestNodeLimit = 5000;
        public int ScheduleCrawlDelayMinSeconds = 1;
        public int ScheduleCrawlDelayMaxSeconds = 3;

        public WebGraphSettings WebGraph { get; set; } = new WebGraphSettings();
    }
}
