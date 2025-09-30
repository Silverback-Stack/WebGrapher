using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Events.Core.Bus
{
    public class AzureServiceBusAdapter : BaseEventBus
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly Dictionary<string, ServiceBusProcessor> _processors = new();
        private readonly int _maxConcurrencyLimitPerEvent;
        private bool _started = false;

        // no need for concurrencyLimits , uses maxConcurrentCalls instead
        public AzureServiceBusAdapter(
            ILogger logger, string connectionString, int maxConcurrencyLimitPerEvent)
            : base(logger) 
        {
            _client = new ServiceBusClient(connectionString);
            _adminClient = new ServiceBusAdministrationClient(connectionString);
            _maxConcurrencyLimitPerEvent = maxConcurrencyLimitPerEvent;
        }

        public override async Task Subscribe<TEvent>(
            string serviceName,
            Func<TEvent, Task> handler)
            where TEvent : class
        {
            var topicName = typeof(TEvent).Name;
            var subscriptionName = serviceName;

            // Ensure topic + subscription exist
            if (!await _adminClient.TopicExistsAsync(topicName))
                await _adminClient.CreateTopicAsync(topicName);

            if (!await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName))
                await _adminClient.CreateSubscriptionAsync(topicName, subscriptionName);

            // Create processor
            var processor = _client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _maxConcurrencyLimitPerEvent,
                AutoCompleteMessages = false
            });

            processor.ProcessMessageAsync += async args =>
            {
                try
                {
                    var body = args.Message.Body.ToArray();
                    var evt = JsonSerializer.Deserialize<TEvent>(body);

                    if (evt != null)
                        await handler(evt);

                    await args.CompleteMessageAsync(args.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling message for {EventType}", typeof(TEvent).Name);
                    await args.AbandonMessageAsync(args.Message);
                }
            };

            processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Service Bus error in {EventType}", typeof(TEvent).Name);
                return Task.CompletedTask;
            };

            _processors[$"{topicName}:{subscriptionName}"] = processor;

            // if bus already started → start this new processor immediately
            if (_started)
                await processor.StartProcessingAsync();

            _logger.LogDebug($"{serviceName} service subscribed to event {typeof(TEvent).Name}");
        }

        public override async Task Unsubscribe<TEvent>(string serviceName, Func<TEvent, Task> handler)
        {
            var key = $"{typeof(TEvent).Name}:{serviceName}";
            if (_processors.TryGetValue(key, out var processor))
            {
                await processor.StopProcessingAsync();
                await processor.DisposeAsync();
                _processors.Remove(key);

                _logger.LogDebug($"{serviceName} service unsubscribed from event {typeof(TEvent).Name}");
            }
        }

        public override async Task PublishAsync<TEvent>(
            TEvent @event,
            int priority = 0,
            DateTimeOffset? scheduledEnqueueTime = null,
            CancellationToken cancellationToken = default)
        {
            var topicName = typeof(TEvent).Name;
            if (!await _adminClient.TopicExistsAsync(topicName))
                await _adminClient.CreateTopicAsync(topicName);

            var sender = _client.CreateSender(topicName);
            var body = JsonSerializer.SerializeToUtf8Bytes(@event);
            var message = new ServiceBusMessage(body);

            if (scheduledEnqueueTime.HasValue)
                message.ScheduledEnqueueTime = scheduledEnqueueTime.Value.UtcDateTime;

            await sender.SendMessageAsync(message, cancellationToken);
        }

        public override async Task StartAsync()
        {
            _started = true;

            //take a snapshow to prevent modifying changing data
            var processorsSnapshot = _processors.Values.ToList();
            foreach (var processor in processorsSnapshot)
                await processor.StartProcessingAsync();
        }

        public override async Task StopAsync()
        {
            foreach (var processor in _processors.Values)
                await processor.StopProcessingAsync();
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var processor in _processors.Values)
                processor.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
