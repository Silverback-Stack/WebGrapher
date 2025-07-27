using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;
using ScraperService;
using Serilog;

namespace WebMapper.Cli
{
    internal class ScraperService
    {
        public async static Task StartAsync(IEventBus eventBus)
        {
            var scraperLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/scraper.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

            var scraperLogger = LoggerFactory.CreateLogger(
                "ScraperService",
                LoggerOptions.Serilog,
                scraperLoggerConfig
            );

            var cache = CacheFactory.CreateCache(
                "ScraperService",
                CacheOptions.InMemory,
                scraperLogger);

            var requestSender = RequestFactory.CreateRequestSender(
                scraperLogger, cache);

            ScraperFactory.Create(scraperLogger, eventBus, requestSender);
        }
    }
}
