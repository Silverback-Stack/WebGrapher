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
        public static IPageParser CreateParser(IAppLogger appLogger, IEventBus eventBus)
        {
            return new HtmlAgilityPackPageParser(appLogger, eventBus);
        }
    }
}
