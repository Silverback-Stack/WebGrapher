using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;

namespace ParserService
{
    public static class ParserFactory
    {
        public static IPageParser CreateParser(ILogger logger, IEventBus eventBus)
        {
            var service = new HtmlAgilityPackPageParser(logger, eventBus);
            service.SubscribeAll();
            return service;
        }
    }
}
