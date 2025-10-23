
using Events.Core.Events.LogEvents;

namespace Events.Core.Bus
{
    public interface IEventBusLifecycle
    {
        /// <summary>
        /// Subscribes to all event handlers.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Unsubscribes and cleans up all event handlers.
        /// </summary>
        Task StopAsync();

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
