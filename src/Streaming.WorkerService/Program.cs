using App.Settings;
using Auth.WebApi;
using Events.Core.Bus;
using Events.Factories;
using Logging.Factories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Streaming.Core;
using Streaming.Factories;
using Streaming.Infrastructure.Adapters.SignalR;
using Streaming.WebApi;

namespace Streaming.WorkerService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Create generic host builder
            var builder = Host.CreateApplicationBuilder(args);

            // Load appsettings.json and environment overrides
            var eventsAppSettings = ConfigurationLoader.LoadConfiguration(path: "Events");
            var streamingAppSettings = ConfigurationLoader.LoadConfiguration(path: "Streaming");
            var authAppSettings = ConfigurationLoader.LoadConfiguration(path: "Auth");

            // Bind configuration overrides onto settings objects
            var eventBusConfig = eventsAppSettings.BindSection<EventsConfig>("Events");
            var streamingConfig = streamingAppSettings.BindSection<StreamingConfig>("Streaming");
            var streamingWebApiConfig = streamingAppSettings.BindSection<StreamingWebApiConfig>("StreamingWebApi");
            var authConfig = authAppSettings.BindSection<AuthConfig>("Auth");


            // Create logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                streamingAppSettings, streamingConfig.Settings.ServiceName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();
            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            // Start SignalR host (Kestrel) and get hub context & URL
            var (hubContext, hubUrl, webHost) = 
                await InitializeSignalRHostAsync(streamingConfig, streamingWebApiConfig, authConfig);


            // Register Event Bus in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventsFactory.CreateEventBus(logger, eventBusConfig);
            });

            // Register Settings in DI
            builder.Services.AddSingleton(streamingWebApiConfig);

            // Register the Streaming Service in DI
            // Pass the HubContext to the StreamerFactory so the streamer can broadcast messages
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IGraphStreamer>>();
                var eventBus = sp.GetRequiredService<IEventBus>();

                var streamerService = StreamingFactory.Create(
                    logger,
                    eventBus,
                    hubContext,
                    streamingConfig);

                return streamerService;
            });


            // Register the Worker background service
            builder.Services.AddHostedService<Worker>();


            // Build and run the host
            var host = builder.Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{streamingConfig.Settings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                if (webHost != null) await webHost.StopAsync();
                Log.CloseAndFlush();
            }
        }


        /// <summary>
        /// Initializes and configures the SignalR hosting environment based on the selected provider.
        /// Returns the hub context, hub URL, and optionally the in-process web host.
        /// </summary>
        private static async Task<(IHubContext<GraphStreamerHub>? hubContext, string hubUrl, IWebHost? webHost)>
            InitializeSignalRHostAsync(
                StreamingConfig streamingConfig,
                StreamingWebApiConfig streamingWebApiConfig,
                AuthConfig authConfig)
        {
            IWebHost? webHost = null;
            IHubContext<GraphStreamerHub>? hubContext = null;
            string hubUrl;

            switch (streamingConfig.Provider)
            {
                case StreamingProvider.SignalRHosted:
                case StreamingProvider.SignalRAzureDefault:
                    // Start local web server for hosted SignalR endpoint
                    webHost = BuildKestrelHost(streamingConfig, streamingWebApiConfig, authConfig);
                    await webHost.StartAsync();
                    hubContext = webHost.Services.GetRequiredService<IHubContext<GraphStreamerHub>>();
                    hubUrl = $"{streamingWebApiConfig.Host}{streamingConfig.SignalR.HubPath}";
                    break;

                case StreamingProvider.SignalRAzureServerless:
                    // Azure serverless: clients connect directly to Azure SignalR endpoint
                    hubUrl = $"{streamingConfig.SignalR.AzureServerless.Endpoint}{streamingConfig.SignalR.HubPath}";
                    break;

                default:
                    throw new NotSupportedException($"SignalR provider '{streamingConfig.Provider}' is not supported.");
            }

            return (hubContext, hubUrl, webHost);
        }

        /// <summary>
        /// Builds an in-process Kestrel host for SignalR.
        /// </summary>
        private static IWebHost BuildKestrelHost(
            StreamingConfig streamingConfig,
            StreamingWebApiConfig streamingWebApiConfig,
            AuthConfig authConfig)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls(streamingWebApiConfig.Host)
                .ConfigureServices(services =>
                {
                    services.AddStreamingWebApi(streamingConfig, streamingWebApiConfig, authConfig);
                })
                .Configure(app =>
                {
                    app.UseStreamingWebApi(streamingConfig, streamingWebApiConfig);
                })
                .Build();
        }
    }
}