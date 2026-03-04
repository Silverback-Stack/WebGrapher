using App.Settings;
using Caching.Factories;
using Events.Core.Bus;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Normalisation.Core;
using Normalisation.Factories;
using Requests.Factories;

namespace WebGrapher.Cli.InProcessHosts
{
    internal class NormalisationHost
    {
        private readonly IEventBus _eventBus;
        private readonly IHostEnvironment _hostEnvironment;

        public NormalisationHost(
            IEventBus eventBus,
            IHostEnvironment hostEnvironment)
        {
            _eventBus = eventBus;
            _hostEnvironment = hostEnvironment;
        }

        public async Task StartAsync()
        {
            // Load appsettings.json and environment overrides
            var appSettings = ConfigurationLoader.LoadConfiguration(
                _hostEnvironment.EnvironmentName, "Logging", "Normalisation");

            // Bind configuration overrides onto settings objects
            var normalisationConfig = appSettings.BindSection<NormalisationConfig>("Normalisation");
            var metaCacheConfig = appSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = appSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = appSettings.BindSection<RequestsConfig>("Requests");

            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLoggerFactory(
                appSettings, 
                normalisationConfig.Settings.ServiceName,
                _hostEnvironment.EnvironmentName);
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
                normalisationConfig.Settings.ServiceName, _hostEnvironment.EnvironmentName);

            // Create Normalisation Service
            var normalisationService = NormalisationFactory.Create(
                logger, _eventBus, requestSender, blobCache, normalisationConfig.Settings);
            await normalisationService.StartAsync();
        }
    }
}
