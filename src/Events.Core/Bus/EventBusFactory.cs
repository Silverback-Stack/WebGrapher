using System;
using Events.Core.Bus.Adapters.Memory;
using Microsoft.Extensions.Logging;

namespace Events.Core.Bus
{
    public static class EventBusFactory
    {
        public static IEventBus Create(ILogger logger, EventBusSettings eventBusSettings)
        {
            switch (eventBusSettings.Provider)
            {
                case EventBusProvider.Memory:
                    return new MemoryEventBusAdapter(
                        logger,
                        eventBusSettings.MemoryEventBus.MaxConcurrencyLimitPerEvent);

                case EventBusProvider.AzureServiceBus:
                    return new AzureServiceBusAdapter(
                        logger,
                        eventBusSettings.AzureServiceBus.ConnectionString,
                        eventBusSettings.AzureServiceBus.MaxConcurrencyLimitPerEvent,
                        eventBusSettings.AzureServiceBus.PrefetchCount);

                default:
                    throw new NotSupportedException($"Event Bus Provider '{eventBusSettings.Provider}' is not supported.");
            }
        }
    }
}
