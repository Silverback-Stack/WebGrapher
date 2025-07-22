using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;

namespace ScraperService
{
    public static class ScraperFactory
    {
        public static IScraper Create(ILogger logger, IEventBus eventbus)
        {
            return new HttpClientScraper(logger, eventbus);
        }
    }
}
