using System;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

namespace Parser.Core
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
