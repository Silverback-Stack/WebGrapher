using System;
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
