namespace Events.Core.Bus
{
    public interface IEventBus : IDisposable
    {
        //TODO: add cancellation tokens
        void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
        Task StartAsync();
        Task StopAsync();
    }
}
