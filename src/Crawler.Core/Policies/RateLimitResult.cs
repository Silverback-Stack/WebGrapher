namespace Crawler.Core.Policies
{
    public class RateLimitResult
    {
        public bool IsRateLimited { get; set; }
        public DateTimeOffset? RetryAfter { get; set; }
    }

}
