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
            CrawlerSettings crawlerSettings,
            ILogger logger,
            IEventBus eventBus,
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver)
        {
            var service = new PageCrawler(
                crawlerSettings, logger, eventBus, requestSender, sitePolicyResolver);

            service.SubscribeAll();
            return service;
        }
    }
}
