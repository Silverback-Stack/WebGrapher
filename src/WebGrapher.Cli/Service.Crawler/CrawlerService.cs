using System;
using Caching.Core;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Settings.Core;

namespace WebGrapher.Cli.Service.Crawler
{
    internal class CrawlerService
    {
        public async static Task<IPageCrawler> InitializeAsync(IEventBus eventBus)
        {
            //Setup Configuration using appsettings overrides
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Crawler");

            //Bind to strongly typed objects
            var crawlerSettings = configuration.BindSection<CrawlerSettings>("Crawler");
            var metaCacheSettings = configuration.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configuration.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configuration.BindSection<RequestSenderSettings>("RequestSender");
            var policyCacheSettings = configuration.BindSection<CacheSettings>("PolicyCache");


            //Setup Logging
            var logFilePath = $"logs/{crawlerSettings.ServiceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IPageCrawler>();


            //Create Meta Cache for Request Sender
            var metaCache = CacheFactory.CreateCache(
                crawlerSettings.ServiceName,
                logger,
                metaCacheSettings);


            //Create Blob Cache for Request Sender
            var blobCache = CacheFactory.CreateCache(
                crawlerSettings.ServiceName,
                logger,
                blobCacheSettings);


            //Create Request Sender

            var requestSender = RequestFactory.CreateRequestSender(
                logger, 
                metaCache, 
                blobCache,
                requestSenderSettings);


            //Create Policy Cache for Site Policy Resolver
            var policyCache = CacheFactory.CreateCache(
                crawlerSettings.ServiceName,
                logger,
                policyCacheSettings);


            //Create Site Policy Resolver
            var sitePolicyResolver = new SitePolicyResolver(
                logger, policyCache, requestSender, crawlerSettings);


            //Create Crawler
            var crawler = CrawlerFactory.CreateCrawler(
                logger, eventBus, requestSender, sitePolicyResolver, crawlerSettings);

            logger.LogInformation($"Crawler service started.");
            return crawler;
        }
    }
}
