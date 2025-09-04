
using Events.Core.Events.LogEvents;

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

        /// <summary>
        /// Publishes a client-facing log entry associated with a specific graph.
        /// This can be used to record informational messages, warnings, or errors
        /// that should be visible to the client consuming the graph.
        /// </summary>
        Task PublishClientLogEventAsync(
            Guid graphId,
            Guid? correlationId,
            LogType type, 
            string message, 
            string? code = null,
            Object? context = null);
    }
}
