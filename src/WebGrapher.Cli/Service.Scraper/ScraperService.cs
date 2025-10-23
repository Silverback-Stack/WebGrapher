using Caching.Core;
using Events.Core.Bus;
using Logger.Core;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;
using Settings.Core;

namespace WebGrapher.Cli.Service.Scraper
{
    internal class ScraperService
    {
        public async static Task InitializeAsync(IEventBus eventBus)
        {
            // Load Configuration
            var configScraper = ConfigurationLoader.LoadConfiguration("Service.Scraper");
            var scraperSettings = configScraper.BindSection<ScraperSettings>("Scraper");
            var metaCacheSettings = configScraper.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configScraper.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configScraper.BindSection<RequestSenderSettings>("RequestSender");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configScraper, scraperSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IRequestSender>();


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


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                scraperSettings.ServiceName, configScraper.GetEnvironmentName());

            // Create Scraper Service
            var scraperService = ScraperFactory.Create(logger, eventBus, requestSender, scraperSettings);

            await scraperService.StartAsync();
        }
    }
}
