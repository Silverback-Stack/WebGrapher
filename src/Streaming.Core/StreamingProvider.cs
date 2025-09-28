
namespace Streaming.Core
{
    public enum StreamingProvider
    {
        HostedSignalR, //Self-hosted SignalR
        AzureSignalRDefault, //Azure-hosted SignalR Default mode
        AzureSignalRServerless //Azure-hosted SignalR Serverless mode (function app only)
    }
}
