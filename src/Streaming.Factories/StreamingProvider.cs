
namespace Streaming.Factories
{
    public enum StreamingProvider
    {
        SignalRHosted, //Self-hosted SignalR
        SignalRAzureDefault, //Azure-hosted SignalR Default mode
        SignalRAzureServerless //Azure-hosted SignalR Serverless mode (function app only)
    }
}
