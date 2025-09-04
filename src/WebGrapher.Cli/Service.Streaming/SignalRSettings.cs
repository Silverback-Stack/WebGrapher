
namespace WebGrapher.Cli.Service.Streaming
{
    public class SignalRSettings
    {
        public string Host { get; set; } = "http://localhost:5001";
        public string HubPath { get; set; } = "/graphstreamerhub";
    }
}
