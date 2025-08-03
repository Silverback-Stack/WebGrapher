using System;
using System.Text;

namespace Crawler.Core
{
    public record SitePolicyItem
    {
        public required string UrlAuthority { get; init; }
        public string? RobotsTxt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
        public DateTimeOffset ModifiedAt { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }

        internal bool IsRateLimited => 
            RetryAfter is not null && DateTimeOffset.UtcNow < this.RetryAfter;

        internal SitePolicyItem MergePolicy(SitePolicyItem other)
        {
            return this with
            {
                RetryAfter = MergeRetryAfter(this.RetryAfter, other.RetryAfter),
                RobotsTxt = MergeRobotsTxtContent(this.RobotsTxt, other.RobotsTxt),
                ModifiedAt = MergeExpiresAt(this.ModifiedAt, other.ModifiedAt)
            };
        }

        private static DateTimeOffset? MergeRetryAfter(DateTimeOffset? a, DateTimeOffset? b)
        {
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;
            return a.Value.CompareTo(b.Value) >= 0 ? a : b;
        }
        private static DateTimeOffset MergeExpiresAt(DateTimeOffset a, DateTimeOffset b)
        {
            return a > b ? a : b; //keep highest value
        }

        private static string? MergeRobotsTxtContent(string? a, string? b)
        {
            return b ?? a;
        }

    }
}
