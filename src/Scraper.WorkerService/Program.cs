using App.Settings;
using Caching.Factories;
using Events.Core.Bus;
using Events.Factories;
using Logging.Factories;
using Requests.Factories;
using Scraper.Core;
using Scraper.Factories;
using Serilog;

namespace Scraper.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var hostEnvironment = builder.Environment;

            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                hostEnvironment.EnvironmentName, "Logging", "Events", "Scraper");

            // Bind configuration overrides onto settings objects
            var eventBusConfig = appSettings.BindSection<EventsConfig>("Events");
            var scraperConfig = appSettings.BindSection<ScraperConfig>("Scraper");
            var metaCacheConfig = appSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = appSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = appSettings.BindSection<RequestsConfig>("Requests");


            // Create Logger
            LoggingFactory.CreateLoggerFactory(
                appSettings, 
                scraperConfig.Settings.ServiceName, 
                hostEnvironment.EnvironmentName);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);


            // Register Event Bus as a singleton in DI
            builder.Services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IEventBus>>();
                return EventsFactory.CreateEventBus(logger, eventBusConfig);
            });


            // Register Scraper Service as a singleton in DI
            builder.Services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IPageScraper>>();

                var eventBus = sp.GetRequiredService<IEventBus>();

                // Create Meta Cache for Request Sender
                var metaCache = CacheFactory.Create(
                    scraperConfig.Settings.ServiceName,
                    logger,
                    metaCacheConfig);


                // Create Blob Cache for Request Sender
                var blobCache = CacheFactory.Create(
                    scraperConfig.Settings.ServiceName,
                    logger,
                    blobCacheConfig);


                // Create Request Sender
                var requestSender = RequestsFactory.Create(
                    logger,
                    metaCache,
                    blobCache,
                    requestsConfig);

                // Create Scraper Service
                var scraperService = ScraperFactory.Create(
                    logger, 
                    eventBus, 
                    requestSender,
                    scraperConfig.Settings);

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
                Log.Fatal(ex, $"{scraperConfig.Settings.ServiceName} terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}