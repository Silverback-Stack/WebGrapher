using Caching.Core;
using Events.Core.Bus;
using Logger.Core;
using Microsoft.Extensions.Logging;
using Normalisation.Core;
using Requests.Core;
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
            var metaCacheSettings = configNormalisation.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configNormalisation.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configNormalisation.BindSection<RequestSenderSettings>("RequestSender");

            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configNormalisation, normalisationSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageNormaliser>();


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


            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                normalisationSettings.ServiceName, configNormalisation.GetEnvironmentName());

            // Create Normalisation Service
            var normalisationService = NormalisationFactory.Create(logger, eventBus, requestSender, blobCache, normalisationSettings);
            await normalisationService.StartAsync();
        }
    }
}
