using System;
using Crawler.Core.SitePolicy;
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
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver,
            CrawlerSettings crawlerSettings)
        {
            var service = new PageCrawler(
                logger, eventBus, requestSender, sitePolicyResolver, crawlerSettings);

            return service;
        }
    }
}
