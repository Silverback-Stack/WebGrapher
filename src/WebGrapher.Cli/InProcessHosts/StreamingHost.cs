using App.Settings;
using Auth.WebApi;
using Events.Core.Bus;
using Logging.Factories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Streaming.Core;
using Streaming.Factories;
using Streaming.Infrastructure.Adapters.SignalR;
using Streaming.WebApi;


namespace WebGrapher.Cli.InProcessHosts
{
    public class StreamingHost
    {
        private readonly IEventBus _eventBus;
        private readonly IHostEnvironment _hostEnvironment;
        private static IHost? _host;

        public StreamingHost(
            IEventBus eventBus,
            IHostEnvironment hostEnvironment)
        {
            _eventBus = eventBus;
            _hostEnvironment = hostEnvironment;
        }

        public async Task StartAsync()
        {
            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                _hostEnvironment.EnvironmentName, "Logging", "Streaming", "Auth");

            // Bind configuration overrides onto settings objects
            var streamingConfig = appSettings.BindSection<StreamingConfig>("Streaming");
            var streamingWebApiConfig = appSettings.BindSection<StreamingWebApiConfig>("StreamingWebApi");          
            var authConfig = appSettings.BindSection<AuthConfig>("Auth");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLoggerFactory(
                appSettings, 
                streamingConfig.Settings.ServiceName,
                _hostEnvironment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            // Start SignalR host and get hub context & URL
            var (host, hubUrl) = await StartSignalRHostAsync(
                streamingConfig, 
                streamingWebApiConfig,
                authConfig);
            _host = host;


            logger.LogInformation(
                "{ServiceName} service is starting using {EnvironmentName} configuration on {HubUrl}",
                streamingConfig.Settings.ServiceName,
                _hostEnvironment.EnvironmentName,
                hubUrl);

            var hubContext = _host.Services.GetRequiredService<IHubContext<GraphStreamerHub>>();


            // Create Streaming Service
            var streamingService = StreamingFactory.Create(
                logger, 
                _eventBus, 
                hubContext,
                streamingConfig);

            await streamingService.StartAsync();
        }

        private async static Task<(IHost host, string hubUrl)> StartSignalRHostAsync(
            StreamingConfig streamingConfig,
            StreamingWebApiConfig streamingWebApiConfig,
            AuthConfig authConfig)
        {
            var builder = WebApplication.CreateBuilder();

            // Use existing logger
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);

            // Add the Streaming API
            builder.Services.AddStreamingWebApi(
                streamingConfig, 
                streamingWebApiConfig, 
                authConfig);
            var app = builder.Build();
            app.UseStreamingWebApi(streamingConfig, streamingWebApiConfig);


            string hubUrl;

            // Configure endpoint host URLs
            switch (streamingConfig.Provider)
            {
                case StreamingProvider.SignalRHosted:
                case StreamingProvider.SignalRAzureDefault:
                    app.Urls.Add(streamingWebApiConfig.Host);
                    hubUrl = $"{streamingWebApiConfig.Host}{streamingConfig.SignalR.HubPath}";
                    break;

                case StreamingProvider.SignalRAzureServerless:
                    hubUrl = $"{streamingConfig.SignalR.AzureServerless.Endpoint}{streamingConfig.SignalR.HubPath}";
                    break;

                default:
                    throw new NotSupportedException($"SignalR provider '{streamingConfig.Provider}' is not supported.");
            }

            await app.StartAsync();

            return (app, hubUrl);
        }

        public static async Task StopSignalRHostAsync()
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
