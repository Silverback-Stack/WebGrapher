using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;

namespace Scraper.Factories
{
    public static class ScraperFactory
    {
        public static IPageScraper Create(ILogger logger, IEventBus eventbus, IRequestSender requestSender, ScraperSettings scraperSettings)
        {
            var service = new PageScraper(logger, eventbus, requestSender, scraperSettings);

            return service;
        }
    }
}
