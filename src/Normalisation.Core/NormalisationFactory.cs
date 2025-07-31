
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

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
