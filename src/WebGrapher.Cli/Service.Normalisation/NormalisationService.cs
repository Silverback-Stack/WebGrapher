using Caching.Core;
using Events.Core.Bus;
using Graphing.Core;
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
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Normalisation");
            var normalisationSettings = configuration.BindSection<NormalisationSettings>("Normalisation");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configuration, normalisationSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageNormaliser>();


            // Create Blob Cache
            var blobCacheSettings = new CacheSettings();
            configuration.GetSection("BlobCache").Bind(blobCacheSettings);
            var cache = CacheFactory.CreateCache(
                normalisationSettings.ServiceName,
                logger,
                blobCacheSettings);


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                normalisationSettings.ServiceName, configuration.GetEnvironmentName());

            // Create Normalisation Service
            var normalisationService = NormalisationFactory.CreateNormaliser(logger, cache, eventBus, normalisationSettings);
            await normalisationService.StartAsync();
        }
    }
}
