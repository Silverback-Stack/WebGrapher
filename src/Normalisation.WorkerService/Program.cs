using Caching.Core;
using Events.Core.Bus;
using Logger.Core;
using Normalisation.Core;
using Requests.Core;
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
            var metaCacheSettings = configNormalisation.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configNormalisation.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configNormalisation.BindSection<RequestSenderSettings>("RequestSender");


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

                // Create Meta Cache for Request Sender
                var metaCache = CacheFactory.Create(
                    normalisationSettings.ServiceName,
                    logger,
                    metaCacheSettings);

                // Create Blob Cache for Request Sender
                var blobCache = CacheFactory.Create(
                    normalisationSettings.ServiceName,
                    logger,
                    blobCacheSettings);

                // Create Request Sender
                var requestSender = RequestFactory.Create(
                    logger,
                    metaCache,
                    blobCache,
                    requestSenderSettings);


                // Create Normalisation Service
                var normalisationService = NormalisationFactory.Create(
                    logger,
                    eventBus,
                    requestSender,
                    blobCache, 
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