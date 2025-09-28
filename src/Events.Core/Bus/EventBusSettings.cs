using System;
using Events.Core.RateLimiters;

namespace Events.Core.Bus
{
    public class EventBusSettings
    {
        public EventBusProvider Provider { get; set; } = EventBusProvider.Memory;

        public string ServiceName { get; set; } = "Events";
        public int MinScheduleDelaySeconds { get; set; } = 1;
        public int MaxScheduleDelaySeconds { get; set; } = 3;

        public RateLimiterSettings RateLimiter { get; set; } = new RateLimiterSettings();
    }
}
