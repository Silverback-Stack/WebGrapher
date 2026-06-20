using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitePolicy.Core
{
    /// <summary>
    /// Represents the current rate limiting state for a site.
    /// </summary>
    public record SiteRateLimitPolicyItem
    {
        public required string UrlAuthority { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset ModifiedAt { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }

        public bool IsRateLimited =>
            RetryAfter is not null && DateTimeOffset.UtcNow < RetryAfter;


        /// <summary>
        /// Merges two rate limiting policies into a single policy using the configured merge rules.
        /// </summary>
        public SiteRateLimitPolicyItem Merge(SiteRateLimitPolicyItem other)
        {
            return this with
            {
                RetryAfter = MergeRetryAfter(RetryAfter, other.RetryAfter),
                ModifiedAt = MergeModifiedAt(ModifiedAt, other.ModifiedAt)
            };
        }


        /// <summary>
        /// Merges Retry-After values by retaining the most restrictive value.
        /// </summary>
        private static DateTimeOffset? MergeRetryAfter(DateTimeOffset? a, DateTimeOffset? b)
        {
            // When only one policy specifies a Retry-After value,
            // that value becomes the merged result.
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;

            // Return the most restrictive value.
            return a.Value >= b.Value ? a : b;
        }

        /// <summary>
        /// Merges modification timestamps by retaining the most recent value.
        /// </summary>
        private static DateTimeOffset MergeModifiedAt(DateTimeOffset a, DateTimeOffset b)
        {
            // Return the most recent value.
            return a > b ? a : b;
        }
    }
}
