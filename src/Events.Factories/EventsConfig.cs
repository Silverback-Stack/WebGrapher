using Events.Core;
using Events.Infrastructure.Bus.Adapters.AzureServiceBus;
using Events.Infrastructure.Bus.Adapters.Memory;
using System;

namespace Events.Factories
{
    public class EventsConfig
    {
        public EventsSettings Settings { get; set; } = new EventsSettings();

        public EventBusProvider Provider { get; set; } = EventBusProvider.Memory;

        public MemoryEventBusSettings MemoryEventBus { get; set; } = new MemoryEventBusSettings();

        public AzureServiceBusSettings AzureServiceBus { get; set; } = new AzureServiceBusSettings();
    }
}
