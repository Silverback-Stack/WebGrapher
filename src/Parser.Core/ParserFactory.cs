using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;

namespace ParserService
{
    public static class ParserFactory
    {
        public static IPageParser Create(IEventBus eventBus)
        {
            return new HtmlAgilityPackPageParser(eventBus);
        }
    }
}
