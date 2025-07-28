using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crawler.Core
{
    public record SitePolicyItem
    {
        public required string UrlAuthority { get; init; }
        public string? RobotsTxtContent { get; init; }
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
                RobotsTxtContent = MergeRobotsTxtContent(this.RobotsTxtContent, other.RobotsTxtContent),
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
