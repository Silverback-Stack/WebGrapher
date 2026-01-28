using App.Settings;
using Caching.Factories;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Crawler.Factories;
using Events.Core.Bus;
using Logging.Factories;
using Microsoft.Extensions.Logging;
using Requests.Factories;
using System;

namespace WebGrapher.Cli.Services
{
    internal class CrawlerService
    {
        public async static Task<IPageCrawler> InitializeAsync(IEventBus eventBus)
        {
            // Load appsettings.json and environment overrides
            var crawlerAppSettings = ConfigurationLoader.LoadConfiguration(path: "Crawler");

            // Bind configuration overrides onto settings objects
            var crawlerConfig = crawlerAppSettings.BindSection<CrawlerConfig>("Crawler");
            var metaCacheConfig = crawlerAppSettings.BindSection<CacheConfig>("MetaCache");
            var blobCacheConfig = crawlerAppSettings.BindSection<CacheConfig>("BlobCache");
            var requestsConfig = crawlerAppSettings.BindSection<RequestsConfig>("Requests");
            var policyCacheConfig = crawlerAppSettings.BindSection<CacheConfig>("PolicyCache");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                crawlerAppSettings, crawlerConfig.Settings.ServiceName);
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
                crawlerConfig.Settings.ServiceName, crawlerAppSettings.GetEnvironmentName());

            // Create Crawler Service
            var crawlerService = CrawlerFactory.Create(
                logger, eventBus, sitePolicyResolver, crawlerConfig);

            await crawlerService.StartAsync();

            return crawlerService;
        }
    }
}
