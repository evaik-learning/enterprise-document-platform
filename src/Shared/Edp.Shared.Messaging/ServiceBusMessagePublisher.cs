using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;

namespace Edp.Shared.Messaging;

public sealed class ServiceBusMessagePublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    private readonly string _queueOrTopicName;

    public ServiceBusMessagePublisher(ServiceBusClient client, string queueOrTopicName)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _queueOrTopicName = string.IsNullOrWhiteSpace(queueOrTopicName)
            ? throw new ArgumentException("A queue or topic name is required.", nameof(queueOrTopicName))
            : queueOrTopicName;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        var message = new ServiceBusMessage(JsonSerializer.SerializeToElement(@event).GetRawText())
        {
            Subject = typeof(T).Name,
            ContentType = "application/json"
        };

        await using var sender = _client.CreateSender(_queueOrTopicName);
        await sender.SendMessageAsync(message, cancellationToken);
    }

    public async Task PublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var payload = JsonSerializer.Serialize(envelope);
        var message = new ServiceBusMessage(payload)
        {
            Subject = envelope.EventType,
            ContentType = "application/json"
        };

        await using var sender = _client.CreateSender(_queueOrTopicName);
        await sender.SendMessageAsync(message, cancellationToken);
    }
}
