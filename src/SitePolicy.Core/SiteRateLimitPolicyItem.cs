using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitePolicy.Core
{
    public record SiteRateLimitPolicyItem
    {
        public required string UrlAuthority { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset ModifiedAt { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }

        public bool IsRateLimited =>
            RetryAfter is not null && DateTimeOffset.UtcNow < RetryAfter;

        public SiteRateLimitPolicyItem Merge(SiteRateLimitPolicyItem other)
        {
            return this with
            {
                RetryAfter = MergeRetryAfter(RetryAfter, other.RetryAfter),
                ModifiedAt = MergeModifiedAt(ModifiedAt, other.ModifiedAt)
            };
        }

        private static DateTimeOffset? MergeRetryAfter(DateTimeOffset? a, DateTimeOffset? b)
        {
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;
            return a.Value >= b.Value ? a : b;
        }
        private static DateTimeOffset MergeModifiedAt(DateTimeOffset a, DateTimeOffset b)
        {
            return a > b ? a : b; //keep highest value
        }
    }
}
