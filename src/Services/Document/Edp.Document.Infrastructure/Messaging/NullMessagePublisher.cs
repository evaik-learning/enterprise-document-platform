using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Edp.Document.Infrastructure.Messaging;

public sealed class NullMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NullMessagePublisher> _logger;

    public NullMessagePublisher(ILogger<NullMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Document messaging is disabled; discarding event of type {EventType}", typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task PublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Document messaging is disabled; discarding event {EventType}", envelope.EventType);
        return Task.CompletedTask;
    }
}
