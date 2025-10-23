using Caching.Core;
using Events.Core.Bus;
using Logger.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normalisation.Core;
using Settings.Core;

namespace WebGrapher.Cli.Service.Normalisation
{
    internal class NormalisationService
    {
        public static async Task InitializeAsync(IEventBus eventBus)
        {
            // Load Configuration
            var configNormalisation = ConfigurationLoader.LoadConfiguration("Service.Normalisation");
            var normalisationSettings = configNormalisation.BindSection<NormalisationSettings>("Normalisation");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configNormalisation, normalisationSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageNormaliser>();


            // Create Blob Cache
            var blobCacheSettings = new CacheSettings();
            configNormalisation.GetSection("BlobCache").Bind(blobCacheSettings);
            var cache = CacheFactory.Create(
                normalisationSettings.ServiceName,
                logger,
                blobCacheSettings);


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                normalisationSettings.ServiceName, configNormalisation.GetEnvironmentName());

            // Create Normalisation Service
            var normalisationService = NormalisationFactory.Create(logger, cache, eventBus, normalisationSettings);
            await normalisationService.StartAsync();
        }
    }
}
