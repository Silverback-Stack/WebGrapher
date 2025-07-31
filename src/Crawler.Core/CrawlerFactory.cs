using System;
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
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
