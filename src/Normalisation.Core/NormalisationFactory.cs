
using Caching.Core;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;
using Requests.Core;

namespace Normalisation.Core
{
    public static class NormalisationFactory
    {
        public static IPageNormaliser Create(ILogger logger, IEventBus eventBus, IRequestSender requestSender, ICache cache, NormalisationSettings normalisationSettings)
        {
            var service = new PageNormaliser(logger, eventBus, requestSender, cache, normalisationSettings);

            return service;
        }
    }
}
