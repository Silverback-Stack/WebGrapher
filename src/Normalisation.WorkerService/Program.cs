using Caching.Core;
using Events.Core.Bus;
using Logger.Core;
using Normalisation.Core;
using Serilog;
using Settings.Core;

namespace Normalisation.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load Configuration
            var configEvents = ConfigurationLoader.LoadConfiguration("Service.Events");
            var eventsSettings = configEvents.BindSection<EventBusSettings>("EventBus");

            var configNormalisation = ConfigurationLoader.LoadConfiguration("Service.Normalisation");
            var normalisationSettings = configNormalisation.BindSection<NormalisationSettings>("Normalisation");
            var blobCacheSettings = configNormalisation.BindSection<CacheSettings>("BlobCache");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configNormalisation, normalisationSettings.ServiceName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();


            // Register Event Bus as a singleton in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventBusFactory.Create(logger, eventsSettings);
            });


            // Register Normalisation Service as a singleton in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageNormaliser>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Blob Cache
                var cache = CacheFactory.Create(
                    normalisationSettings.ServiceName,
                    logger,
                    blobCacheSettings);

                // Create Normalisation Service
                var normalisationService = NormalisationFactory.Create(
                    logger, 
                    cache, 
                    eventBus, 
                    normalisationSettings);

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
                Log.Fatal(ex, $"{normalisationSettings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }
    }
}