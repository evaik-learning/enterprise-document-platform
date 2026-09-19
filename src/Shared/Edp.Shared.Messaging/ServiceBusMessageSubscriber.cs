using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Edp.Shared.Messaging;

public sealed class ServiceBusMessageSubscriber : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusMessageSubscriber> _logger;
    private readonly List<ServiceBusProcessor> _processors = [];

    public ServiceBusMessageSubscriber(
        ServiceBusClient client,
        ILogger<ServiceBusMessageSubscriber> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SubscribeAsync(
        string topicName,
        string subscriptionName,
        IMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        ArgumentNullException.ThrowIfNull(handler);

        var processor = _client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 4
        });

        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(args.Message.Body)
                    ?? throw new InvalidOperationException("Service Bus message envelope could not be deserialized.");

                await handler.HandleAsync(envelope, args.Message.MessageId, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process Service Bus message {MessageId} from {Topic}/{Subscription}.", args.Message.MessageId, topicName, subscriptionName);
                if (args.Message.DeliveryCount >= 10)
                {
                    await args.DeadLetterMessageAsync(args.Message, "Maximum delivery attempts exceeded.");
                }
                else
                {
                    await args.AbandonMessageAsync(args.Message);
                }
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus processor error for {Topic}/{Subscription}.", topicName, subscriptionName);
            return Task.CompletedTask;
        };

        _processors.Add(processor);
        await processor.StartProcessingAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var processor in _processors)
        {
            await processor.DisposeAsync();
        }

        _processors.Clear();
    }
}
