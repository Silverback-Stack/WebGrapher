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
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Crawler");
            var crawlerSettings = configuration.BindSection<CrawlerSettings>("Crawler");
            var metaCacheSettings = configuration.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configuration.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configuration.BindSection<RequestSenderSettings>("RequestSender");
            var policyCacheSettings = configuration.BindSection<CacheSettings>("PolicyCache");


            // Create Logger
            ILoggerFactory loggerFactory = LoggingFactory.CreateLogger(
                configuration, crawlerSettings.ServiceName);
            var logger = loggerFactory.CreateLogger<IPageCrawler>();


            // Create Meta Cache for Request Sender
            var metaCache = CacheFactory.CreateCache(
                crawlerSettings.ServiceName,
                logger,
                metaCacheSettings);


            // Create Blob Cache for Request Sender
            var blobCache = CacheFactory.CreateCache(
                crawlerSettings.ServiceName,
                logger,
                blobCacheSettings);


            // Create Request Sender
            var requestSender = RequestFactory.CreateRequestSender(
                logger,
                metaCache,
                blobCache,
                requestSenderSettings);


            // Create Policy Cache for Site Policy Resolver
            var policyCache = CacheFactory.CreateCache(
                crawlerSettings.ServiceName,
                logger,
                policyCacheSettings);


            // Create Site Policy Resolver
            var sitePolicyResolver = new SitePolicyResolver(
                logger, policyCache, requestSender, crawlerSettings);



            logger.LogInformation("{ServiceName} service is starting using {EnvironmentName} configuration.",
                crawlerSettings.ServiceName, configuration.GetEnvironmentName());

            // Create Crawler Service
            var crawler = CrawlerFactory.CreateCrawler(
                logger, eventBus, requestSender, sitePolicyResolver, crawlerSettings);

            await crawler.StartAsync();

            return crawler;
        }
    }
}
