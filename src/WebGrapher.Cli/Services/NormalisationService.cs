using App.Settings;
using Caching.Factories;
using Events.Core.Bus;
using Logging.Factories;
using Microsoft.Extensions.Logging;
using Normalisation.Core;
using Normalisation.Factories;
using Requests.Factories;

namespace WebGrapher.Cli.Services
{
    internal class NormalisationService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            // Load appsettings.json and environment overrides
            var normalisationAppSettings = ConfigurationLoader.LoadConfiguration(path: "Normalisation");

            // Bind configuration overrides onto settings objects
            var normalisationConfig = normalisationAppSettings.BindSection<NormalisationConfig>("Normalisation");
            var metaCacheConfig = normalisationAppSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = normalisationAppSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = normalisationAppSettings.BindSection<RequestsConfig>("Requests");

            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                normalisationAppSettings, normalisationConfig.Settings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageNormaliser>();


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


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                normalisationConfig.Settings.ServiceName, normalisationAppSettings.GetEnvironmentName());

            // Create Normalisation Service
            var normalisationService = NormalisationFactory.Create(logger, eventBus, requestSender, blobCache, normalisationConfig.Settings);
            await normalisationService.StartAsync();
        }
    }
}
