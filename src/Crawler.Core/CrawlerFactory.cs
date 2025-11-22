using System;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Crawler.Core
{
    public class CrawlerFactory
    {
        public static IPageCrawler Create(
            ILogger logger,
            IEventBus eventBus,
            ISitePolicyResolver sitePolicyResolver,
            CrawlerSettings crawlerSettings)
        {
            var service = new PageCrawler(
                logger, eventBus, sitePolicyResolver, crawlerSettings);

            return service;
        }
    }
}
