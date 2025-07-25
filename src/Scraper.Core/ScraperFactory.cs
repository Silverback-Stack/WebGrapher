using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;
using Requests.Core;
using Scraper.Core;

namespace ScraperService
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
