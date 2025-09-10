
namespace Events.Core.RateLimiters
{
    public class RateLimiterSettings
    {
        public int MaxCrawlPageEvents { get; set; } = 20;
        public int MaxScrapePageEvents { get; set; } = 20;
        public int MaxNormalisePageEvents { get; set; } = 20;
        public int MaxGraphPageEvents { get; set; } = 20;
        public int MaxGraphNodeAddedEvents { get; set; } = 20;
    }
}
