using System;
using Crawler.Core;
using Crawler.Core.SitePolicy;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

namespace Crawler.Factories
{
    public class CrawlerFactory
    {
        public static IPageCrawler Create(
            ILogger logger,
            IEventBus eventBus,
            ISitePolicyResolver sitePolicyResolver,
            CrawlerConfig crawlerConfig)
        {
            return new PageCrawler(
                logger, 
                eventBus, 
                sitePolicyResolver, 
                crawlerConfig.Settings);
        }
    }
}
