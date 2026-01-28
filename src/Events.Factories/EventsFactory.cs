using System;
using Events.Core.Bus;
using Events.Infrastructure.Bus;
using Events.Infrastructure.Bus.Adapters.Memory;
using Microsoft.Extensions.Logging;

namespace Events.Factories
{
    public static class EventsFactory
    {
        public static IEventBus CreateEventBus(ILogger logger, EventsConfig eventsConfig)
        {
            switch (eventsConfig.Provider)
            {
                case EventBusProvider.Memory:
                    return new MemoryEventBusAdapter(
                        logger,
                        eventsConfig.MemoryEventBus.MaxConcurrencyLimitPerEvent);

                case EventBusProvider.AzureServiceBus:
                    return new AzureServiceBusAdapter(
                        logger,
                        eventsConfig.AzureServiceBus.ConnectionString,
                        eventsConfig.AzureServiceBus.MaxConcurrencyLimitPerEvent,
                        eventsConfig.AzureServiceBus.PrefetchCount);

                default:
                    throw new NotSupportedException($"Event Bus Provider '{eventsConfig.Provider}' is not supported.");
            }
        }
    }
}
