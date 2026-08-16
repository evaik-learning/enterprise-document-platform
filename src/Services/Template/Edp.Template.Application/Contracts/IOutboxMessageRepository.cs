namespace Edp.Template.Application.Contracts;

public interface IOutboxMessageRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount = 20, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default);
}

public sealed class OutboxMessage
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

    public static OutboxMessage Create(string eventType, string aggregateType, Guid? aggregateId, object payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);

        return new OutboxMessage
        {
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload),
            OccurredOnUtc = DateTimeOffset.UtcNow,
            RetryCount = 0
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
