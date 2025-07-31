using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;
using Scraper.Core;

namespace Scraper.Core
{
    public static class ScraperFactory
    {
        public static IScraper Create(ILogger logger, IEventBus eventbus, IRequestSender requestSender)
        {
            var service = new PageScraper(logger, eventbus, requestSender);
            service.SubscribeAll();
            return service;
        }
    }
}
