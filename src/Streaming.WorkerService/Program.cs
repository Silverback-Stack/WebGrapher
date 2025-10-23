using Events.Core.Bus;
using Logger.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Settings.Core;
using Streaming.Core;
using Streaming.Core.Adapters.SignalR;
using Streaming.WebApi;

namespace Streaming.WorkerService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Create generic host builder
            var builder = Host.CreateApplicationBuilder(args);


            // Load configurations
            var configEvents = ConfigurationLoader.LoadConfiguration("Service.Events");
            var eventsSettings = configEvents.BindSection<EventBusSettings>("EventBus");

            var configStreaming = ConfigurationLoader.LoadConfiguration("Service.Streaming");
            var streamingSettings = configStreaming.BindSection<StreamingSettings>("Streaming");


            // Create logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configStreaming, streamingSettings.ServiceName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();
            var logger = loggerFactory.CreateLogger<IGraphStreamer>();


            // Start SignalR host (Kestrel) and get hub context & URL
            var (hubContext, hubUrl, webHost) = await InitializeSignalRHostAsync(streamingSettings);


            // Register Event Bus in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventBusFactory.Create(logger, eventsSettings);
            });


            // Register the Streaming Service in DI
            // Pass the HubContext to the StreamerFactory so the streamer can broadcast messages
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IGraphStreamer>>();
                var eventBus = sp.GetRequiredService<IEventBus>();

                var streamerService = StreamerFactory.Create(
                    logger,
                    eventBus,
                    hubContext,
                    streamingSettings);

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
                Log.Fatal(ex, $"{streamingSettings.ServiceName} terminated unexpectedly.");
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
            InitializeSignalRHostAsync(StreamingSettings streamingSettings)
        {
            IWebHost? webHost = null;
            IHubContext<GraphStreamerHub>? hubContext = null;
            string hubUrl;

            switch (streamingSettings.Provider)
            {
                case StreamingProvider.HostedSignalR:
                case StreamingProvider.AzureSignalRDefault:
                    // Start local web server for hosted SignalR endpoint
                    webHost = BuildKestrelHost(streamingSettings);
                    await webHost.StartAsync();
                    hubContext = webHost.Services.GetRequiredService<IHubContext<GraphStreamerHub>>();
                    hubUrl = $"{streamingSettings.HostedSignaR.Host}{streamingSettings.HubPath}";
                    break;

                case StreamingProvider.AzureSignalRServerless:
                    // Azure serverless: clients connect directly to Azure SignalR endpoint
                    hubUrl = $"{streamingSettings.AzureSignalRServerless.Endpoint}{streamingSettings.HubPath}";
                    break;

                default:
                    throw new NotSupportedException($"SignalR provider '{streamingSettings.Provider}' is not supported.");
            }

            return (hubContext, hubUrl, webHost);
        }

        /// <summary>
        /// Builds an in-process Kestrel host for SignalR.
        /// </summary>
        private static IWebHost BuildKestrelHost(StreamingSettings streamingSettings)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls(streamingSettings.HostedSignaR.Host)
                .ConfigureServices(services =>
                {
                    services.AddStreamingWebApi(streamingSettings);
                })
                .Configure(app =>
                {
                    app.UseStreamingWebApi(streamingSettings);
                })
                .Build();
        }
    }
}