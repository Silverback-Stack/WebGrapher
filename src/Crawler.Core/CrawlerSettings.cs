
using SitePolicy.Core;

namespace Crawler.Core
{
    public class CrawlerSettings
    {
        public string ServiceName { get; set; } = "Crawler";

        public int MaxCrawlAttemptLimit { get; set; } = 3;

        public int MaxCrawlDepthLimit { get; set; } = 3;

        public int ScheduleCrawlDelayMinSeconds { get; set; } = 1;

        public int ScheduleCrawlDelayMaxSeconds { get; set; } = 3;

        public int DefaultRetryDelaySeconds { get; set; } = 300;

        public SitePolicySettings SitePolicy { get; set; } = new SitePolicySettings();
    }
}
