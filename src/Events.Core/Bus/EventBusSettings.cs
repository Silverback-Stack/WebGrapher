using System;
using Events.Core.Bus.Adapters.AzureServiceBus;
using Events.Core.Bus.Adapters.Memory;

namespace Events.Core.Bus
{
    public class EventBusSettings
    {
        public string ServiceName { get; set; } = "Events";
        public EventBusProvider Provider { get; set; } = EventBusProvider.Memory;

        public MemoryEventBusSettings MemoryEventBus { get; set; } = new MemoryEventBusSettings();

        public AzureServiceBusSettings AzureServiceBus { get; set; } = new AzureServiceBusSettings();
    }
}
