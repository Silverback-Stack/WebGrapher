using Events.Core.Bus;
using Logger.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Settings.Core;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;
using Streaming.WebApi;


namespace WebGrapher.Cli.Service.Streaming
{
    public class StreamingService
    {
        private static IHost? _host;

        public static async Task InitializeAsync(IEventBus eventBus)
        {
            //Load Configuration
            var configStreaming = ConfigurationLoader.LoadConfiguration("Service.Streaming");
            var streamingSettings = configStreaming.BindSection<StreamingSettings>("Streaming");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configStreaming, streamingSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            // Start SignalR host and get hub context & URL
            var (host, hubUrl) = await InitializeSignalRHostAsync(streamingSettings);
            _host = host;


            logger.LogInformation(
                "{ServiceName} service is starting using {EnvironmentName} configuration on {HubUrl}",
                streamingSettings.ServiceName,
                configStreaming.GetEnvironmentName(),
                hubUrl);

            var hubContext = _host.Services.GetRequiredService<IHubContext<GraphStreamerHub>>();


            // Create Streaming Service
            var streamingService = StreamerFactory.Create(
                logger, 
                eventBus, 
                hubContext, 
                streamingSettings);

            await streamingService.StartAsync();
        }

        private async static Task<(IHost host, string hubUrl)> InitializeSignalRHostAsync(
            StreamingSettings settings)
        {
            var builder = WebApplication.CreateBuilder();

            // Use existing logger
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);

            // Add the Streaming API
            builder.Services.AddStreamingWebApi(settings);
            var app = builder.Build();
            app.UseStreamingWebApi(settings);


            string hubUrl;

            // Configure endpoint host URLs
            switch (settings.Provider)
            {
                case StreamingProvider.HostedSignalR:
                case StreamingProvider.AzureSignalRDefault:
                    app.Urls.Add(settings.HostedSignaR.Host);
                    hubUrl = $"{settings.HostedSignaR.Host}{settings.HubPath}";
                    break;

                case StreamingProvider.AzureSignalRServerless:
                    hubUrl = $"{settings.AzureSignalRServerless.Endpoint}{settings.HubPath}";
                    break;

                default:
                    throw new NotSupportedException($"SignalR provider '{settings.Provider}' is not supported.");
            }

            await app.StartAsync();

            return (app, hubUrl);
        }

        public static async Task StopHubServerAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                await _host.WaitForShutdownAsync();
                _host.Dispose();
            }
            Log.CloseAndFlush();
        }

    }

}
