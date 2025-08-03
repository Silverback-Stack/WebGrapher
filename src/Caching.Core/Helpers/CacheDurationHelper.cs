
namespace Caching.Core.Helpers
{
    public static class CacheDurationHelper
    {
        private const int DEFAULT_MIN_ABSOLUTE_EXPIRY_MINUTES = 5;
        private const int DEFAULT_MAX_ABSOLUTE_EXPIRY_MINUTES = 20;

        /// <summary>
        /// Returns a clamped duration between now and the provided expiry,
        /// within configured bounds. Returns minimal TTL if expiry is missing or already expired.
        /// </summary>
        public static TimeSpan? Clamp(
            DateTimeOffset? expires, 
            int minDurationInMinutes = DEFAULT_MIN_ABSOLUTE_EXPIRY_MINUTES, 
            int maxDurationInMinutes = DEFAULT_MAX_ABSOLUTE_EXPIRY_MINUTES)
        {
            if (minDurationInMinutes > maxDurationInMinutes)
                throw new ArgumentException("minDuration cannot be greater than maxDuration");

            if (!expires.HasValue || 
                expires <= DateTimeOffset.UtcNow)
            {
                //no expiry provided, default to minimum TTL
                return TimeSpan.FromMinutes(minDurationInMinutes);
            }

            var cacheDuration = expires.Value - DateTimeOffset.UtcNow;
            var minDuration = TimeSpan.FromMinutes(minDurationInMinutes);
            var maxDuration = TimeSpan.FromMinutes(maxDurationInMinutes);

            //override expires within range of values
            if (cacheDuration > maxDuration) cacheDuration = maxDuration;
            if (cacheDuration < minDuration) cacheDuration = minDuration;

            return cacheDuration > TimeSpan.Zero ? cacheDuration : null;
        }
    }
}
