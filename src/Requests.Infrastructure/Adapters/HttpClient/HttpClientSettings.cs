using System;

namespace Requests.Infrastructure.Adapters.HttpClient
{
    public class HttpClientSettings
    {
        public bool AllowAutoRedirect { get; set; } = true;
        public int MaxAutomaticRedirections { get; set; } = 5;
        public int TimeoutSeconds { get; set; } = 10;
        public int RetryAfterFallbackMinutes { get; set; } = 5;
    }
}
