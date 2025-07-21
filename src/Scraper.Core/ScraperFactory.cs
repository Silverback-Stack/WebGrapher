using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;

namespace ScraperService
{
    public static class ScraperFactory
    {
        public static IScraper Create(IEventBus eventbus)
        {
            return new HttpClientScraper(eventbus);
        }
    }
}
