using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
using Crawler.Core.RobotsEvaluator;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;

namespace Crawler.Core
{
    public class CrawlerFactory
    {
        public static ICrawler CreateCrawler(
            CrawlerOptions storeType,
            IAppLogger logger,
            IEventBus eventBus,
            ICache cache,
            IRequestSender requestSender,
            IRobotsEvaluator robotsEvaluator)
        {
            switch (storeType)
            {
                case CrawlerOptions.InMemory:
                    return new MemoryCacheCrawlerAdapter(
                        logger, eventBus, cache, requestSender, robotsEvaluator);

                case CrawlerOptions.LiteDb:
                    return new LiteDbCrawlerAdapter(
                        logger, eventBus, cache, requestSender, robotsEvaluator);

                default:
                    throw new ArgumentOutOfRangeException(nameof(storeType),
                        $"Unsupported store type: {storeType}");
            }
        }
    }
}
