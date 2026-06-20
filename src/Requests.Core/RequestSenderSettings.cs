using System;

namespace Requests.Core
{
    public class RequestSenderSettings
    {
        public int CacheMinAbsoluteExpiryMinutes { get; set; } = 5;
        public int CacheMaxAbsoluteExpiryMinutes { get; set; } = 20;

        /// <summary>
        /// Optional key used to group RequestSender instances that share the same outbound identity.
        /// Request Senders using the same key belong to the same group.
        /// If not set, each RequestSender instance uses a unique key and operates independently.
        /// </summary>
        public string GroupKey { get; set; } = string.Empty;
    }
}
