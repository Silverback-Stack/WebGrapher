
using Crawler.Core.SitePolicy;

namespace Crawler.Core
{
    public class CrawlerSettings
    {
        public string ServiceName { get; set; } = "CRAWLER";
        public int MaxCrawlAttemptLimit { get; set; } = 3;
        public int MaxCrawlDepthLimit { get; set; } = 3;

        public SitePolicySettings SitePolicy { get; set; } = new SitePolicySettings();
    }
}
