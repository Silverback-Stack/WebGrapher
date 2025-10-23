using Caching.Core;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Logger.Core;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Settings.Core;
using System;

namespace WebGrapher.Cli.Service.Crawler
{
    internal class CrawlerService
    {
        public async static Task<IPageCrawler> InitializeAsync(IEventBus eventBus)
        {
            // Load Configuration
            var configCrawler = ConfigurationLoader.LoadConfiguration("Service.Crawler");
            var crawlerSettings = configCrawler.BindSection<CrawlerSettings>("Crawler");
            var metaCacheSettings = configCrawler.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configCrawler.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configCrawler.BindSection<RequestSenderSettings>("RequestSender");
            var policyCacheSettings = configCrawler.BindSection<CacheSettings>("PolicyCache");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configCrawler, crawlerSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageCrawler>();


            // Create Meta Cache for Request Sender
            var metaCache = CacheFactory.Create(
                crawlerSettings.ServiceName,
                logger,
                metaCacheSettings);


            // Create Blob Cache for Request Sender
            var blobCache = CacheFactory.Create(
                crawlerSettings.ServiceName,
                logger,
                blobCacheSettings);


            // Create Request Sender
            var requestSender = RequestFactory.Create(
                logger,
                metaCache,
                blobCache,
                requestSenderSettings);


            // Create Policy Cache for Site Policy Resolver
            var policyCache = CacheFactory.Create(
                crawlerSettings.ServiceName,
                logger,
                policyCacheSettings);


            // Create Site Policy Resolver
            var sitePolicyResolver = new SitePolicyResolver(
                logger, policyCache, requestSender, crawlerSettings);



            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                crawlerSettings.ServiceName, configCrawler.GetEnvironmentName());

            // Create Crawler Service
            var crawlerService = CrawlerFactory.Create(
                logger, eventBus, requestSender, sitePolicyResolver, crawlerSettings);

            await crawlerService.StartAsync();

            return crawlerService;
        }
    }
}
