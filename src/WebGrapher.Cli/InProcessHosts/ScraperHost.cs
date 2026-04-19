using App.Settings;
using Caching.Factories;
using Events.Core.Bus;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Requests.Factories;
using Scraper.Factories;
using SitePolicy.Core;

namespace WebGrapher.Cli.InProcessHosts
{
    public class ScraperHost
    {
        private readonly IEventBus _eventBus;
        private readonly IHostEnvironment _hostEnvironment;

        public ScraperHost(
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
                _hostEnvironment.EnvironmentName, "Logging", "Scraper");

            // Bind configuration overrides onto settings objects
            var scraperConfig = appSettings.BindSection<ScraperConfig>("Scraper");
            var metaCacheConfig = appSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = appSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = appSettings.BindSection<RequestsConfig>("Requests");
            var policyCacheConfig = appSettings.BindSection<CacheConfig>("PolicyCache");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLoggerFactory(
                appSettings, 
                scraperConfig.Settings.ServiceName,
                _hostEnvironment.EnvironmentName);
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


            // Create Policy Cache for Site Policy Resolver
            var policyCache = CacheFactory.Create(
                scraperConfig.Settings.ServiceName,
                logger,
                policyCacheConfig);


            // Create Site Policy Resolver
            var sitePolicyResolver = new SitePolicyResolver(
                logger, policyCache, requestSender, scraperConfig.Settings.SitePolicy);



            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                scraperConfig.Settings.ServiceName, _hostEnvironment.EnvironmentName);

            // Create Scraper Service
            var scraperService = ScraperFactory.Create(
                logger, _eventBus, requestSender, sitePolicyResolver, scraperConfig.Settings);

            await scraperService.StartAsync();
        }
    }
}
