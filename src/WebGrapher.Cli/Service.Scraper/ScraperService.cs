using System;
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;
using Serilog;
using Serilog.Extensions.Logging;
using Settings.Core;

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

            // Setup Serilog Logging
            var serilogLogger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(
                    path: $"logs/{scraperSettings.ServiceName}.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
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

            logger.LogInformation("{ServiceName} service started with environment {Environment}",
                scraperSettings.ServiceName,
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));
        }
    }
}
