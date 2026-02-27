using App.Settings;
using Caching.Factories;
using Events.Core.Bus;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Requests.Factories;
using Scraper.Factories;

namespace WebGrapher.Cli.Services
{
    internal class ScraperService
    {
        public async static Task InitializeAsync(
            IEventBus eventBus,
            IHostEnvironment environment)
        {
            // Load appsettings.json and environment overrides
            var scraperAppSettings = ConfigurationLoader.LoadConfiguration(
                environment.EnvironmentName, "Logging", "Scraper");

            // Bind configuration overrides onto settings objects
            var scraperConfig = scraperAppSettings.BindSection<ScraperConfig>("Scraper");
            var metaCacheConfig = scraperAppSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = scraperAppSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = scraperAppSettings.BindSection<RequestsConfig>("Requests");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                scraperAppSettings, 
                scraperConfig.Settings.ServiceName, 
                environment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<IRequestSender>();


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


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                scraperConfig.Settings.ServiceName, environment.EnvironmentName);

            // Create Scraper Service
            var scraperService = ScraperFactory.Create(
                logger, eventBus, requestSender, scraperConfig.Settings);

            await scraperService.StartAsync();
        }
    }
}
