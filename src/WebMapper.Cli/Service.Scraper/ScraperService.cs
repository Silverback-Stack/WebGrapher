using System;
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Scraper
{
    internal class ScraperService
    {
        public async static Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(ScraperService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(serilogLogger);

            var logger = loggerFactory.CreateLogger<IRequestSender>();

            var metaCache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InMemory,
                logger);

            var blobCache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InStorage,
                logger);

            var requestSender = RequestFactory.CreateRequestSender(logger, metaCache, blobCache);

            ScraperFactory.Create(logger, eventBus, requestSender);

            logger.LogInformation("Scraper service started.");
        }
    }
}
