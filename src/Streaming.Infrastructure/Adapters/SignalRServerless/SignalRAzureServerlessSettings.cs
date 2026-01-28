using System;

namespace Streaming.Infrastructure.Adapters.SignalRServerless
{
    public class SignalRAzureServerlessSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }
}
