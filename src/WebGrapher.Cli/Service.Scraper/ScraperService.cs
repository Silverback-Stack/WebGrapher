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
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Scraper");
            var scraperSettings = configuration.BindSection<ScraperSettings>("Scraper");
            var metaCacheSettings = configuration.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configuration.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configuration.BindSection<RequestSenderSettings>("RequestSender");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configuration, scraperSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IRequestSender>();


            // Create Meta Cache for Request Sender
            var metaCache = CacheFactory.CreateCache(
                scraperSettings.ServiceName,
                logger,
                metaCacheSettings);


            // Create Blob Cache for Request Sender
            var blobCache = CacheFactory.CreateCache(
                scraperSettings.ServiceName,
                logger,
                blobCacheSettings);


            // Create Request Sender
            var requestSender = RequestFactory.CreateRequestSender(
                logger, 
                metaCache, 
                blobCache,
                requestSenderSettings);


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                scraperSettings.ServiceName, configuration.GetEnvironmentName());

            // Create Scraper Service
            var scraperService = ScraperFactory.Create(logger, eventBus, requestSender, scraperSettings);
            await scraperService.StartAsync();
        }
    }
}
