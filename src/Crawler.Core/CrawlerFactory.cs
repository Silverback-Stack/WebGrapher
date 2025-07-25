using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Crawler.Core.Policies;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core
{
    public class CrawlerFactory
    {
        public static IPageCrawler CreateCrawler(
            ILogger logger,
            IEventBus eventBus,
            ICache cache,
            IRequestSender requestSender)
        {
            IHistoryPolicy historyPolicy = new HistoryPolicy(logger, cache, requestSender);
            IRateLimitPolicy rateLimitPolicy = new RateLimitPolicy(logger, cache, requestSender);
            IRobotsPolicy robotsPolicy = new RobotsPolicy(logger, cache, requestSender);

            var service = new PageCrawler(
                logger, eventBus, cache, requestSender, historyPolicy, rateLimitPolicy, robotsPolicy);

            service.SubscribeAll();
            return service;
        }
    }
}
