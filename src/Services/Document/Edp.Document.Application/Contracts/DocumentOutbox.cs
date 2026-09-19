namespace Edp.Document.Application.Contracts;

public interface IDocumentOutboxMessageRepository
{
    Task AddAsync(DocumentOutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentOutboxMessage>> GetPendingAsync(int maxCount = 20, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default);
}

public sealed class DocumentOutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EventType { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public Guid? AggregateId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    public static DocumentOutboxMessage Create(string eventType, string aggregateType, Guid? aggregateId, object payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentNullException.ThrowIfNull(payload);

        return new DocumentOutboxMessage
        {
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload)
        };
    }

    public void MarkProcessed()
    {
        ProcessedOnUtc = DateTimeOffset.UtcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
