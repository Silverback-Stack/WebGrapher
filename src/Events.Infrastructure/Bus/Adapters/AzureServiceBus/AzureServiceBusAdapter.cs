using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Events.Core.Bus;
using Microsoft.Extensions.Logging;

namespace Events.Infrastructure.Bus
{
    public class AzureServiceBusAdapter : BaseEventBus
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();
        private readonly int _maxConcurrencyLimitPerEvent;
        private readonly int _maxDeliveryCount;
        private readonly int _prefetchCount;
        private bool _started = false;

        public AzureServiceBusAdapter(
            ILogger logger,
            string connectionString, 
            int maxConcurrencyLimitPerEvent,
            int maxDeliveryCount,
            int prefetchCount)
            : base(logger) 
        {
            _client = new ServiceBusClient(connectionString);
            _adminClient = new ServiceBusAdministrationClient(connectionString);
            _maxConcurrencyLimitPerEvent = maxConcurrencyLimitPerEvent;
            _maxDeliveryCount = maxDeliveryCount;
            _prefetchCount = prefetchCount;
        }

        public override async Task Subscribe<TEvent>(
            string serviceName, 
            Func<TEvent, Task> handler) where TEvent : class
        {
            var topicName = typeof(TEvent).Name;
            var subscriptionName = serviceName;
            var key = GetKey<TEvent>(serviceName);

            // Prevent duplicate processor creation (one processor per service + event type)
            if (_processors.ContainsKey(key))
                return;

            // Ensure the messaging infrastructure exists (topic + subscription).
            // In Azure, delivery guarantees (retry, DLQ, durability) are configured here.
            await EnsureTopicAndSubscriptionAsync(topicName, subscriptionName);

            var processor = _client.CreateProcessor(
                topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _maxConcurrencyLimitPerEvent,
                AutoCompleteMessages = false,
                PrefetchCount = _prefetchCount
            });


            // ------------------------------------------------------------
            // Event Dispatch Flow (Azure Adapter)
            // ------------------------------------------------------------
            // 1. Message is received from Azure Service Bus
            // 2. Deserialize payload
            // 3. Dispatch to handler
            // 4. On success  -> CompleteMessageAsync (acknowledges message)
            // 5. On failure  -> AbandonMessageAsync (triggers retry)
            // 6. If retries exceed MaxDeliveryCount -> broker moves to DLQ
            //
            // The core contract only defines "success" or "failure".
            // Delivery guarantees are provided by Azure Service Bus.
            // ------------------------------------------------------------

            processor.ProcessMessageAsync += async args =>
            {
                try
                {
                    var evt = await DeserializeOrDeadLetterAsync<TEvent>(args);

                    await handler(evt);

                    await HandleSuccessAsync(args);
                }
                catch (JsonException)
                {
                    // already dead-lettered in DeserializeOrDeadLetterAsync
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler failed for {EventType}", typeof(TEvent).Name);
                    await HandleFailureAsync(args, ex);
                }
            };

            // Infrastructure-level errors (network, connection, etc.)
            processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Service Bus error in {EventType}", typeof(TEvent).Name);
                return Task.CompletedTask;
            };

            if (!_processors.TryAdd(key, processor))
            {
                await processor.DisposeAsync();
                return;
            }

            if (_started)
                await processor.StartProcessingAsync();

            _logger.LogDebug($"{serviceName} service subscribed to event {typeof(TEvent).Name}");
        }


        public override async Task Unsubscribe<TEvent>(string serviceName, Func<TEvent, Task> handler)
        {
            var key = GetKey<TEvent>(serviceName);

            if (_processors.TryRemove(key, out var processor))
            {
                await processor.StopProcessingAsync();
                await processor.DisposeAsync();

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

            await using var sender = _client.CreateSender(topicName);
            var body = JsonSerializer.SerializeToUtf8Bytes(@event);
            var message = new ServiceBusMessage(body);

            if (scheduledEnqueueTime.HasValue)
                message.ScheduledEnqueueTime = scheduledEnqueueTime.Value.UtcDateTime;

            await sender.SendMessageAsync(message, cancellationToken);
        }


        public override async Task StartAsync()
        {
            _started = true;

            //take a snapshot to prevent modifying changing data
            var processorsSnapshot = _processors.Values.ToList();

            foreach (var processor in processorsSnapshot)
            {
                if (processor != null)
                    await processor.StartProcessingAsync();
            }
                
        }


        public override async Task StopAsync()
        {
            //take a snapshot to prevent modifying changing data
            var processorsSnapshot = _processors.Values.ToList();

            foreach (var processor in processorsSnapshot)
                if (processor != null)
                    await processor.StopProcessingAsync();

        }


        public override void Dispose()
        {
            base.Dispose();

            // Stop processors first
            foreach (var processor in _processors.Values)
            {
                try { processor.StopProcessingAsync().GetAwaiter().GetResult(); }
                catch { /* ignore shutdown noise */ }
            }

            // Dispose processors
            foreach (var processor in _processors.Values)
                processor.DisposeAsync().AsTask().GetAwaiter().GetResult();

            _processors.Clear();

            // Dispose clients
            _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static Task HandleSuccessAsync(ProcessMessageEventArgs args)
            => args.CompleteMessageAsync(args.Message);

        private static Task HandleFailureAsync(ProcessMessageEventArgs args, Exception ex)
            => args.AbandonMessageAsync(args.Message);

        private static string GetTopicName<TEvent>() where TEvent : class
            => typeof(TEvent).Name;

        private string GetKey<TEvent>(string serviceName) where TEvent : class
            => $"{GetTopicName<TEvent>()}:{serviceName}";

        private async Task EnsureTopicAndSubscriptionAsync(string topicName, string subscriptionName)
        {
            if (!await _adminClient.TopicExistsAsync(topicName))
                await _adminClient.CreateTopicAsync(topicName);

            if (!await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName))
            {
                var subOptions = new CreateSubscriptionOptions(topicName, subscriptionName)
                {
                    MaxDeliveryCount = _maxDeliveryCount
                };

                await _adminClient.CreateSubscriptionAsync(subOptions);
            }
        }

        private async Task<TEvent> DeserializeOrDeadLetterAsync<TEvent>(ProcessMessageEventArgs args)
            where TEvent : class
        {
            try
            {
                var body = args.Message.Body.ToArray();
                var evt = JsonSerializer.Deserialize<TEvent>(body);

                if (evt is null)
                    throw new JsonException($"Deserialized {typeof(TEvent).Name} was null.");

                return evt;
            }
            catch (JsonException ex)
            {
                await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", ex.Message);
                throw; // ensure we don't Complete
            }
        }
    }
}
