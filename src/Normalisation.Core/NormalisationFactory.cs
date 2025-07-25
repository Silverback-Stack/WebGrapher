using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Events.Core.Bus;
using Logging.Core;

namespace Normalisation.Core
{
    public static class NormalisationFactory
    {
        public static IHtmlNormalisation CreateNormaliser(ILogger logger, IEventBus eventBus)
        {
            var service = new HtmlNormalisation(logger, eventBus);
            service.SubscribeAll();
            return service;
        }
    }
}
