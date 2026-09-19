using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Edp.Shared.Messaging;

public sealed class NullMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NullMessagePublisher> _logger;

    public NullMessagePublisher(ILogger<NullMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Service Bus not configured; discarding event {EventType}.", typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task PublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Service Bus not configured; discarding event {EventType}.", envelope.EventType);
        return Task.CompletedTask;
    }
}
