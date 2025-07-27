using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Crawler.Core;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;
using Serilog;

namespace WebMapper.Cli
{
    internal class CrawlerService
    {
        public async static Task StartAsync(IEventBus eventBus)
        {
            var crawlerLoggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/crawler.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                .CreateLogger();

            var crawlerLogger = LoggerFactory.CreateLogger(
                "CrawlerService",
                LoggerOptions.Serilog,
                crawlerLoggerConfig
            );

            var cache = CacheFactory.CreateCache(
                "CrawlerService",
                CacheOptions.InMemory,
                crawlerLogger);

            var requestSender = RequestFactory.CreateRequestSender(
                crawlerLogger, cache);

            CrawlerFactory.CreateCrawler(
                crawlerLogger, eventBus, cache, requestSender);
        }
    }
}
