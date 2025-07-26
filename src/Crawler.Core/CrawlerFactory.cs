using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caching.Core;
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
            ISitePolicyResolver sitePolicyResolver = new SitePolicyResolver(logger, requestSender);

            var service = new PageCrawler(
                logger, eventBus, cache, requestSender, sitePolicyResolver);

            service.SubscribeAll();
            return service;
        }
    }
}
