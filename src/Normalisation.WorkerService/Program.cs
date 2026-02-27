using App.Settings;
using Caching.Factories;
using Events.Core.Bus;
using Events.Factories;
using Logging.Factories;
using Normalisation.Core;
using Normalisation.Factories;
using Requests.Factories;
using Serilog;

namespace Normalisation.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var environment = builder.Environment;

            // Load appsettings.json and environment overrides
            var eventsAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Events");
            var normalisationAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Normalisation");

            // Bind configuration overrides onto settings objects
            var eventBusConfig = eventsAppSettings.BindSection<EventsConfig>("Events");
            var normalisationConfig = normalisationAppSettings.BindSection<NormalisationConfig>("Normalisation");
            var metaCacheConfig = normalisationAppSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = normalisationAppSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = normalisationAppSettings.BindSection<RequestsConfig>("Requests");


            // Create Logger
            LoggingFactory.CreateLogger(
                normalisationAppSettings, 
                normalisationConfig.Settings.ServiceName,
                environment.EnvironmentName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);


            // Register Event Bus as a singleton in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventsFactory.CreateEventBus(logger, eventBusConfig);
            });


            // Register Normalisation Service as a singleton in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageNormaliser>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Meta Cache for Request Sender
                var metaCache = CacheFactory.Create(
                    normalisationConfig.Settings.ServiceName,
                    logger,
                    metaCacheConfig);

                // Create Blob Cache for Request Sender
                var blobCache = CacheFactory.Create(
                    normalisationConfig.Settings.ServiceName,
                    logger,
                    blobCacheConfig);

                // Create Request Sender
                var requestSender = RequestsFactory.Create(
                    logger,
                    metaCache,
                    blobCache,
                    requestsConfig);


                // Create Normalisation Service
                var normalisationService = NormalisationFactory.Create(
                    logger,
                    eventBus,
                    requestSender,
                    blobCache,
                    normalisationConfig.Settings);

                return normalisationService;
            });


            // Add Worker background service
            builder.Services.AddHostedService<Worker>();

            // Build and run
            var host = builder.Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{normalisationConfig.Settings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }
    }
}