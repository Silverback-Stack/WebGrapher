
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

namespace Normalisation.Core
{
    public static class NormalisationFactory
    {
        public static IPageNormaliser CreateNormaliser(ILogger logger, ICache cache, IEventBus eventBus)
        {
            var service = new PageNormaliser(logger, cache, eventBus);
            service.SubscribeAll();
            return service;
        }
    }
}
