using System;
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Configuration;
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
            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Service.Scraper/appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            //bind appsettings overrides to default settings objects
            var scraperSettings = new ScraperSettings();
            configuration.GetSection("Scraper").Bind(scraperSettings);


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
            var metaCacheSettings = new CacheSettings();
            configuration.GetSection("MetaCache").Bind(metaCacheSettings);
            var metaCache = CacheFactory.CreateCache(
                metaCacheSettings,
                scraperSettings.ServiceName,
                logger);


            //Create Blob Cache for Request Sender
            var blobCacheSettings = new CacheSettings();
            configuration.GetSection("BlobCache").Bind(blobCacheSettings);
            var blobCache = CacheFactory.CreateCache(
                blobCacheSettings,
                scraperSettings.ServiceName,
                logger);


            //Create Request Sender
            var requestSenderSettings = new RequestSenderSettings();
            configuration.GetSection("RequestSender").Bind(requestSenderSettings);
            var requestSender = RequestFactory.CreateRequestSender(
                requestSenderSettings,
                logger, 
                metaCache, 
                blobCache);

            ScraperFactory.Create(scraperSettings, logger, eventBus, requestSender);

            logger.LogInformation("Scraper service started.");
        }
    }
}
