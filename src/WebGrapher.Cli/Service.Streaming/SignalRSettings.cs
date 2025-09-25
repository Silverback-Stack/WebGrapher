
namespace WebGrapher.Cli.Service.Streaming
{
    public class SignalRSettings
    {
        public ServiceType Service { get; set; } = ServiceType.AzureDefault;
        public string HubPath { get; set; } = "/graphstreamerhub";

        public HostedSettings Hosted { get; set; } = new HostedSettings();
        public AzureSettings Azure { get; set; } = new AzureSettings();
    }

    public enum ServiceType
    {
        Hosted, //Self-hosted
        AzureDefault, //Azure-hosted default mode
        AzureServerless //Azure-hosted serverless mode (function app only)
    }

    public class HostedSettings
    {
        public string Host { get; set; } = "http://localhost:5100";
    }

    public class AzureSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }
}
