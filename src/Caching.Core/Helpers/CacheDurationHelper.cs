using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caching.Core.Helpers
{
    public static class CacheDurationHelper
    {
        private const int DEFAULT_MAX_ABSOLUTE_EXPIRY_MINUTES = 20;

        /// <summary>
        /// Clamps the expiry duration to the maximul value specified.
        /// </summary>
        public static TimeSpan? Clamp(
            DateTimeOffset? expires, int maxDurationInMinutes = DEFAULT_MAX_ABSOLUTE_EXPIRY_MINUTES)
        {
            if (!expires.HasValue) return null;

            if (expires <= DateTimeOffset.UtcNow) return null;

            var cacheDuration = expires.Value - DateTimeOffset.UtcNow;
            var maxDuration = TimeSpan.FromMinutes(maxDurationInMinutes);

            //override expires if greater than our own
            if (cacheDuration > maxDuration)
                cacheDuration = maxDuration;

            return cacheDuration > TimeSpan.Zero ? cacheDuration : null;
        }
    }
}
