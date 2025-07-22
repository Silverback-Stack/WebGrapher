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
        public static IHtmlNormalisation CreateNormaliser(IAppLogger appLogger, IEventBus eventBus)
        {
            return new HtmlNormalisation(appLogger, eventBus);
        }
    }
}
