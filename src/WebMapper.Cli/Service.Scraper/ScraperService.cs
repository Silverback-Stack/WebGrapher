using System;
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;
using Serilog;
using Serilog.Extensions.Logging;

namespace WebMapper.Cli.Service.Scraper
{
    internal class ScraperService
    {
        public async static Task InitializeAsync(IEventBus eventBus)
        {
            var serviceName = typeof(ScraperService).Name;
            var logFilePath = $"logs/{serviceName}.log";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                //.WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);

            var logger = loggerFactory.CreateLogger<IRequestSender>();

            var cache = CacheFactory.CreateCache(
                serviceName,
                CacheOptions.InMemory,
                logger);

            var requestSender = RequestFactory.CreateRequestSender(logger, cache);

            ScraperFactory.Create(logger, eventBus, requestSender);
        }
    }
}
