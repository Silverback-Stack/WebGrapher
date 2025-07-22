using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;
using ScraperService;
using Serilog;

namespace WebMapper.Cli
{
    internal class ScraperService
    {
        public async static Task ConfigureAsync(IEventBus eventBus)
        {
            var scraperLoggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/scraper.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .CreateLogger();

            var scraperLogger = LoggerFactory.CreateLogger(
                "ScraperService",
                LoggerOptions.Serilog,
                scraperLoggerConfig
            );

            var service = ScraperFactory.Create(scraperLogger, eventBus);
            service.Start();

        }
    }
}
