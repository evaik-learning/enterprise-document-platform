using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Edp.SharedKernel.Domain;
using Edp.Template.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace Edp.Template.Infrastructure.Messaging;

/// <summary>Maps Template domain events to platform integration events and publishes them via the shared message bus.</summary>
public sealed class TemplateEventPublisher : IIntegrationEventPublisher
{
    private readonly IOutboxMessageRepository _outbox;
    private readonly ILogger<TemplateEventPublisher> _logger;

    public TemplateEventPublisher(IOutboxMessageRepository outbox, ILogger<TemplateEventPublisher> logger)
    {
        _outbox = outbox;
        _logger = logger;
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var envelope = new EventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = domainEvent.GetType().Name,
            OccurredAt = DateTimeOffset.UtcNow,
            Data = domainEvent
        };

        try
        {
            var aggregateId = domainEvent switch
            {
                var value when value.GetType().GetProperty("TemplateId") is not null => value.GetType().GetProperty("TemplateId")?.GetValue(value) as Guid?,
                _ => null
            };

            var outboxMessage = OutboxMessage.Create(envelope.EventType, "Template", aggregateId ?? Guid.Empty, envelope);
            await _outbox.AddAsync(outboxMessage, cancellationToken);
            _logger.LogInformation("Persisted {EventType} to outbox for template dispatch.", envelope.EventType);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to persist {EventType} to the outbox", envelope.EventType);
        }
    }

    public async Task PublishRangeAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await PublishAsync(domainEvent, cancellationToken);
        }
    }
}
