using System;

namespace Requests.Core
{
    public class RequestSenderSettings
    {
        public int CacheMinAbsoluteExpiryMinutes { get; set; } = 5;
        public int CacheMaxAbsoluteExpiryMinutes { get; set; } = 20;
    }
}
