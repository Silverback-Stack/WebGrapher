using System;
using Caching.Core;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Microsoft.Extensions.Configuration;
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
            //CONFIG SETTINGS:
            var crawlerConfig = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, "Service.Crawler"))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var crawlerSettings = new CrawlerSettings();
            crawlerConfig.GetSection("CrawlerSettings").Bind(crawlerSettings);


            //SETUP LOGGING:
            var serviceName = crawlerSettings.ServiceName;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IPageCrawler>();


            //Create Request Sender:
            var metaCache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InMemory, //fastest - only good for small data
                logger);

            var blobCache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InStorage, //slower - good for large data
                logger);

            var requestSender = RequestFactory.CreateRequestSender(
                logger, metaCache, blobCache);


            //Create Policy Resolver:
            var policyCache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InMemory,
                logger);

            var sitePolicyResolver = new SitePolicyResolver(
                crawlerSettings, logger, policyCache, requestSender);


            //Create Crawler:
            var crawler = CrawlerFactory.CreateCrawler(
                crawlerSettings, logger, eventBus, requestSender, sitePolicyResolver);

            logger.LogInformation($"Crawler service started.");
            return crawler;
        }
    }
}
