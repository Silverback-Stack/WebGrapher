
namespace Requests.Core
{
    public class RequestSenderSettings
    {
        public bool AllowAutoRedirect { get; set; } = true;
        public int MaxAutomaticRedirections { get; set; } = 5;
        public int TimoutSeconds { get; set; } = 10;
        public int RetryAfterFallbackMinutes { get; set; } = 5;
        public int MinAbsoluteExpiryMinutes { get; set; } = 5;
        public int MaxAbsoluteExpiryMinutes { get; set; } = 20;
    }
}
