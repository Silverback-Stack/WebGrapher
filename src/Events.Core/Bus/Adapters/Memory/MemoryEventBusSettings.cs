
namespace Events.Core.Bus.Adapters.Memory
{
    public class MemoryEventBusSettings
    {
        public int MaxConcurrencyLimitPerEvent { get; set; } = 5;
    }
}
