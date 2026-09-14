using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Edp.Workflow.Infrastructure.Messaging;

public sealed class NullMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NullMessagePublisher> _logger;

    public NullMessagePublisher(ILogger<NullMessagePublisher> logger) => _logger = logger;

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Service Bus not configured; discarding Workflow event {EventType}", typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task PublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Service Bus not configured; discarding Workflow event {EventType}", envelope.EventType);
        return Task.CompletedTask;
    }
}
