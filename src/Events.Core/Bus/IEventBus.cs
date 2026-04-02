namespace Events.Core.Bus
{
    public interface IEventBus : IDisposable
    {
        /// <summary>
        /// Subscribes a service to an event type by registering a handler 
        /// that will be invoked when the event is published.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
        /// <param name="serviceName">The name of the subscribing service.</param>
        /// <param name="handler">The asynchronous function that processes the event when it is received.</param>
        /// <returns></returns>
        Task SubscribeAsync<TEvent>(
            string serviceName, 
            Func<TEvent, Task> handler) where TEvent : class;


        /// <summary>
        /// Unsubscribes a service from an event type by removing the registered handler.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>
        /// <param name="serviceName">The name of the subscribing service.</param>
        /// <param name="handler">The handler function to remove.</param>
        Task UnsubscribeAsync<TEvent>(
            string serviceName, 
            Func<TEvent, Task> handler) where TEvent : class;


        /// <summary>
        /// Publishes an event so it can be processed by all subscribed services.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish.</typeparam>
        /// <param name="event">The event instance to publish.</param>
        /// <param name="priority">The priority of the event. Lower values are processed before higher values.</param>
        /// <param name="scheduledEnqueueTime">An optional time to delay processing until a future point.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns></returns>
        Task PublishAsync<TEvent>(
            TEvent @event, 
            int priority = 0,
            DateTimeOffset? scheduledEnqueueTime = null, 
            CancellationToken cancellationToken = default) where TEvent : class;


        /// <summary>
        /// Starts the event bus and begins processing events.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops the event bus and halts event processing.
        /// </summary>
        Task StopAsync();
    }
}
