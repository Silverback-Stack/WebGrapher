
namespace Requests.Core
{
    public class RequestSenderSettings
    {
        public int RetryAfterFallbackMinutes { get; set; } = 5;
        public int MinAbsoluteExpiryMinutes { get; set; } = 5;
        public int MaxAbsoluteExpiryMinutes { get; set; } = 20;
    }
}
