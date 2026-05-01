using Crawler.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using SitePolicy.Core;
using System;

namespace Crawler.Factories
{
    public class CrawlerFactory
    {
        public static IPageCrawler Create(
            ILogger logger,
            IEventBus eventBus,
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver,
            CrawlerSettings crawlerSettings)
        {
            return new PageCrawler(
                logger, 
                eventBus, 
                requestSender,
                sitePolicyResolver,
                crawlerSettings);
        }
    }
}
