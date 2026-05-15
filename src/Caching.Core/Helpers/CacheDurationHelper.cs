
namespace Caching.Core.Helpers
{
    public static class CacheDurationHelper
    {
        /// <summary>
        /// Returns a clamped duration between now and the provided expiry,
        /// within configured bounds. Returns minimal TTL if expiry is missing or already expired.
        /// </summary>
        public static TimeSpan? Clamp(
            DateTimeOffset? expires, 
            int minDurationInMinutes, 
            int maxDurationInMinutes)
        {
            // Ensure min duration is not greater than max allowed duration.
            if (minDurationInMinutes > maxDurationInMinutes)
                throw new ArgumentException("minDuration cannot be greater than maxDuration");

            // If expiry is missing, or expired, fall back to min cache duration.
            if (!expires.HasValue || expires <= DateTimeOffset.UtcNow)
                //no expiry provided, default to minimum TTL
                return TimeSpan.FromMinutes(minDurationInMinutes);

            // Calculate remaining cache duration.
            var cacheDuration = expires.Value - DateTimeOffset.UtcNow;

            // Convert bounds to TimeSpan values.
            var minDuration = TimeSpan.FromMinutes(minDurationInMinutes);
            var maxDuration = TimeSpan.FromMinutes(maxDurationInMinutes);

            // Clamp duration within configured bounds.
            if (cacheDuration > maxDuration) cacheDuration = maxDuration;
            if (cacheDuration < minDuration) cacheDuration = minDuration;

            // Return valid positive duration or null if no valid cache duration.
            return cacheDuration > TimeSpan.Zero ? cacheDuration : null;
        }
    }
}
