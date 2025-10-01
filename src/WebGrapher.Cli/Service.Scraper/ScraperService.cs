using System;
using Caching.Core;
using Config.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebGrapher.Cli.Service.Scraper
{
    internal class ScraperService
    {
        public async static Task InitializeAsync(IEventBus eventBus)
        {
            //Setup Configuration using appsettings overrides
            var configuration = ConfigurationLoader.LoadConfiguration("Service.Scraper");

            //bind appsettings overrides to default settings objects
            var scraperSettings = configuration.BindSection<ScraperSettings>("Scraper");
            var metaCacheSettings = configuration.BindSection<CacheSettings>("MetaCache");
            var blobCacheSettings = configuration.BindSection<CacheSettings>("BlobCache");
            var requestSenderSettings = configuration.BindSection<RequestSenderSettings>("RequestSender");


            //Setup Logging
            var logFilePath = $"logs/{scraperSettings.ServiceName}.log";
            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);
            var logger = loggerFactory.CreateLogger<IRequestSender>();


            //Create Meta Cache for Request Sender
            var metaCache = CacheFactory.CreateCache(
                scraperSettings.ServiceName,
                logger,
                metaCacheSettings);


            //Create Blob Cache for Request Sender
            var blobCache = CacheFactory.CreateCache(
                scraperSettings.ServiceName,
                logger,
                blobCacheSettings);


            //Create Request Sender
            var requestSender = RequestFactory.CreateRequestSender(
                logger, 
                metaCache, 
                blobCache,
                requestSenderSettings);

            ScraperFactory.Create(logger, eventBus, requestSender, scraperSettings);

            logger.LogInformation("Scraper service started.");
        }
    }
}
