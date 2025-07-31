
namespace Events.Core.Bus
{
    public interface IEventBusLifecycle
    {
        /// <summary>
        /// Registers event handlers or subscriptions for this service.
        /// </summary>
        void SubscribeAll();

        /// <summary>
        /// Unsubscribes or cleans up event handlers.
        /// </summary>
        void UnsubscribeAll();

    }
}
