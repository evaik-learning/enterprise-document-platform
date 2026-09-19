using Edp.Document.Application.Interfaces;
using Edp.Document.Application.Contracts;
using Edp.Shared.Contracts;
using Edp.SharedKernel.Domain;
using Microsoft.Extensions.Logging;

namespace Edp.Document.Infrastructure.Messaging;

public sealed class DocumentEventPublisher : IEventPublisher
{
    private readonly IDocumentOutboxMessageRepository _outbox;
    private readonly ILogger<DocumentEventPublisher> _logger;

    public DocumentEventPublisher(IDocumentOutboxMessageRepository outbox, ILogger<DocumentEventPublisher> logger)
    {
        _outbox = outbox;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        var organizationId = ReadGuid(@event, "OrganizationId");
        var aggregateId = ReadGuid(@event, "DocumentId");
        var envelope = new EventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = @event.GetType().Name,
            OccurredAt = DateTimeOffset.UtcNow,
            OrganizationId = organizationId,
            Data = @event
        };

        await _outbox.AddAsync(
            DocumentOutboxMessage.Create(envelope.EventType, nameof(DomainEvent), aggregateId, envelope),
            cancellationToken);
        _logger.LogInformation("Persisted {EventType} to the document outbox.", envelope.EventType);
    }

    private static Guid? ReadGuid(object value, string propertyName) =>
        value.GetType().GetProperty(propertyName)?.GetValue(value) is Guid id && id != Guid.Empty ? id : null;
}
