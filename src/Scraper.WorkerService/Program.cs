using Caching.Core;
using Events.Core.Bus;
using Logger.Core;
using Requests.Core;
using Scraper.Core;
using Serilog;
using Settings.Core;

namespace Scraper.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load configurations
            var configEvents = ConfigurationLoader.LoadConfiguration("Service.Events");
            var eventsSettings = configEvents.BindSection<EventBusSettings>("EventBus");

            var configScraper = ConfigurationLoader.LoadConfiguration("Service.Scraper");
            var scraperSettings = configScraper.BindSection<ScraperSettings>("Scraper");
            var metaCacheSettings = configScraper.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configScraper.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configScraper.BindSection<RequestSenderSettings>("RequestSender");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configScraper, scraperSettings.ServiceName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();


            // Register Event Bus as a singleton in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventBusFactory.Create(logger, eventsSettings);
            });


            // Register Scraper Service as a singleton in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageScraper>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Meta Cache for Request Sender
                var metaCache = CacheFactory.Create(
                    scraperSettings.ServiceName,
                    logger,
                    metaCacheSettings);


                // Create Blob Cache for Request Sender
                var blobCache = CacheFactory.Create(
                    scraperSettings.ServiceName,
                    logger,
                    blobCacheSettings);


                // Create Request Sender
                var requestSender = RequestFactory.Create(
                    logger,
                    metaCache,
                    blobCache,
                    requestSenderSettings);

                // Create Scraper Service
                var scraperService = ScraperFactory.Create(
                    logger, 
                    eventBus, 
                    requestSender, 
                    scraperSettings);

                return scraperService;
            });


            // Add Worker background service
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{scraperSettings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}