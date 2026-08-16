using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Edp.Template.Infrastructure.Messaging;

/// <summary>No-op publisher used when Service Bus is not configured (local development without messaging infrastructure).</summary>
public sealed class NullMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NullMessagePublisher> _logger;

    public NullMessagePublisher(ILogger<NullMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Service Bus not configured; discarding event of type {EventType}", typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task PublishEnvelopeAsync(Edp.Shared.Contracts.EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Service Bus not configured; discarding event {EventType}", envelope.EventType);
        return Task.CompletedTask;
    }
}
