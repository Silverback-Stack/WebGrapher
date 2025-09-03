using System;
using Caching.Core;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebGrapher.Cli.Service.Crawler
{
    internal class CrawlerService
    {
        public async static Task<IPageCrawler> InitializeAsync(IEventBus eventBus)
        {
            //Setup Configuration using appsettings overrides
            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Service.Crawler/appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            //bind appsettings overrides to default settings objects
            var crawlerSettings = new CrawlerSettings();
            configuration.GetSection("Crawler").Bind(crawlerSettings);


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
            var metaCacheSettings = new CacheSettings();
            configuration.GetSection("MetaCache").Bind(metaCacheSettings);
            var metaCache = CacheFactory.CreateCache(
                metaCacheSettings,
                crawlerSettings.ServiceName,
                logger);


            //Create Blob Cache for Request Sender
            var blobCacheSettings = new CacheSettings();
            configuration.GetSection("BlobCache").Bind(blobCacheSettings);
            var blobCache = CacheFactory.CreateCache(
                blobCacheSettings,
                crawlerSettings.ServiceName,
                logger);


            //Create Request Sender
            var requestSenderSettings = new RequestSenderSettings();
            configuration.GetSection("RequestSender").Bind(requestSenderSettings);
            var requestSender = RequestFactory.CreateRequestSender(
                requestSenderSettings, 
                logger, 
                metaCache, 
                blobCache);


            //Create Policy Cache for Site Policy Resolver
            var policyCacheSettings = new CacheSettings();
            configuration.GetSection("PolicyCache").Bind(policyCacheSettings);
            var policyCache = CacheFactory.CreateCache(
                policyCacheSettings,
                crawlerSettings.ServiceName,
                logger);


            //Create Site Policy Resolver
            var sitePolicyResolver = new SitePolicyResolver(
                crawlerSettings, logger, policyCache, requestSender);


            //Create Crawler
            var crawler = CrawlerFactory.CreateCrawler(
                crawlerSettings, logger, eventBus, requestSender, sitePolicyResolver);

            logger.LogInformation($"Crawler service started.");
            return crawler;
        }
    }
}
