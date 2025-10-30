
namespace Events.Core.Bus.Adapters.AzureServiceBus
{
    public class AzureServiceBusSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int MaxConcurrencyLimitPerEvent { get; set; } = 5;
        public int PrefetchCount { get; set; } = 10;
    }
}
