using System;

namespace Requests.Core
{
    public class RequestSenderSettings
    {
        public int CacheMinAbsoluteExpiryMinutes { get; set; } = 5;
        public int CacheMaxAbsoluteExpiryMinutes { get; set; } = 20;

        /// <summary>
        /// Optional key used to group services under the same rate-limit partition.
        /// Services with the same value will share rate-limit state (e.g. same IP or region).
        /// If not set, each RequestSender instance uses a unique key and is rate-limited independently.
        /// </summary>
        public string RateLimitGroupKey { get; set; } = string.Empty;
    }
}
