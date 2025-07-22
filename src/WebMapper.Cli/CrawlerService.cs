using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Crawler.Core.RobotsEvaluator;
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
            using var crawlerLoggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/crawler.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var crawlerLogger = AppLoggerFactory.CreateLogger(
                "CrawlerService",
                AppLoggerOptions.Serilog,
                crawlerLoggerConfig
            );

            var cache = CacheFactory.CreateCache(
                CacheOptions.InMemory,
                crawlerLogger);

            var requestSender = RequestFactory.CreateRequestSender(
                crawlerLogger);

            var robotsEvaluator = RobotsFactory.CreateRobotsEvaluator(
            crawlerLogger, cache, requestSender);

            var crawlerService = CrawlerFactory.CreateCrawler(
                CrawlerOptions.InMemory, crawlerLogger, eventBus, cache, requestSender, robotsEvaluator);

            await crawlerService.StartAsync();
        }
    }
}
