using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;
using SitePolicy.Core;
using System;

namespace Scraper.Factories
{
    public static class ScraperFactory
    {
        public static IPageScraper Create(
            ILogger logger, 
            IEventBus eventbus, 
            IRequestSender requestSender,
            ISitePolicyResolver sitePolicyResolver,
            ScraperSettings scraperSettings)
        {
            var service = new PageScraper(
                logger, 
                eventbus, 
                requestSender,
                sitePolicyResolver,
                scraperSettings);

            return service;
        }
    }
}
