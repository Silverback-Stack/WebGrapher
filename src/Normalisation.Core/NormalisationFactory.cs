
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

namespace Normalisation.Core
{
    public static class NormalisationFactory
    {
        public static IHtmlNormalisation CreateNormaliser(ILogger logger, ICache cache, IEventBus eventBus)
        {
            var service = new HtmlNormalisation(logger, cache, eventBus);
            service.SubscribeAll();
            return service;
        }
    }
}
