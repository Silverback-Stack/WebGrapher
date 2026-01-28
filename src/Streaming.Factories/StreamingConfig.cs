using Streaming.Core;
using Streaming.Infrastructure.Adapters.SignalR;
using Streaming.Infrastructure.Adapters.SignalRServerless;

namespace Streaming.Factories
{
    public class StreamingConfig
    {
        public StreamingSettings Settings { get; set; } = new();

        public StreamingProvider Provider { get; set; } = StreamingProvider.SignalRHosted;

        public SignalRSettings SignalR { get; set; } = new();

    }

    public class SignalRSettings
    {
        public string HubPath { get; set; } = "/graphstreamerhub";

        public SignalRAzureDefaultSettings AzureDefault { get; set; } = new();

        public SignalRAzureServerlessSettings AzureServerless { get; set; } = new();
    }

}
