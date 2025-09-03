using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Scraper.Core
{
    public static class ScraperFactory
    {
        public static IPageScraper Create(ScraperSettings settings, ILogger logger, IEventBus eventbus, IRequestSender requestSender)
        {
            var service = new PageScraper(settings, logger, eventbus, requestSender);
            service.SubscribeAll();
            return service;
        }
    }
}
