using App.Settings;
using Caching.Factories;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Crawler.Factories;
using Events.Core.Bus;
using Logging.Factories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requests.Factories;
using System;

namespace WebGrapher.Cli.InProcessHosts
{
    public class CrawlerHost
    {
        private readonly IEventBus _eventBus;
        private readonly IHostEnvironment _hostEnvironment;

        public CrawlerHost(
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
                _hostEnvironment.EnvironmentName, "Logging", "Crawler");

            // Bind configuration overrides onto settings objects
            var crawlerConfig = appSettings.BindSection<CrawlerConfig>("Crawler");
            var metaCacheConfig = appSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = appSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = appSettings.BindSection<RequestsConfig>("Requests");
            var policyCacheConfig = appSettings.BindSection<CacheConfig>("PolicyCache");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLoggerFactory(
                appSettings, 
                crawlerConfig.Settings.ServiceName,
                _hostEnvironment.EnvironmentName);
            var logger = loggerFactory.CreateLogger<IPageCrawler>();


            // Create Meta Cache for Request Sender
            var metaCache = CacheFactory.Create(
                crawlerConfig.Settings.ServiceName,
                logger,
                metaCacheConfig);


            // Create Blob Cache for Request Sender
            var blobCache = CacheFactory.Create(
                crawlerConfig.Settings.ServiceName,
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
                crawlerConfig.Settings.ServiceName,
                logger,
                policyCacheConfig);


            // Create Site Policy Resolver
            var sitePolicyResolver = new SitePolicyResolver(
                logger, policyCache, requestSender, crawlerConfig.Settings);



            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                crawlerConfig.Settings.ServiceName, _hostEnvironment.EnvironmentName);

            // Create Crawler Service
            var crawlerService = CrawlerFactory.Create(
                logger, _eventBus, sitePolicyResolver, crawlerConfig);

            await crawlerService.StartAsync();
        }
    }
}
