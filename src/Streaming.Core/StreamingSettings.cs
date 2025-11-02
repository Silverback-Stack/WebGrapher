
namespace Streaming.Core
{
    public class StreamingSettings
    {
        public string ServiceName { get; set; } = "Streaming";
        public string HubPath { get; set; } = "/graphstreamerhub";

        public SignalRSettings SignalR { get; set; } = new SignalRSettings();
    }

    public class SignalRSettings
    {
        public StreamingProvider Provider { get; set; } = StreamingProvider.HostedSignalR;

        public HostedSignaRSettings HostedSignaR { get; set; } = new HostedSignaRSettings();

        public AzureSignalRDefaultSettings AzureSignalRDefault { get; set; } = new AzureSignalRDefaultSettings();

        public AzureSignalRServerlessSettings AzureSignalRServerless { get; set; } = new AzureSignalRServerlessSettings();

    }

    public class HostedSignaRSettings
    {
        public string Host { get; set; } = "http://localhost:5100";
    }

    public class AzureSignalRDefaultSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }

    public class AzureSignalRServerlessSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }
}
