namespace Edp.DigitalSignature.Infrastructure.Outbox;

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

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(string eventType, string aggregateType, Guid? aggregateId, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Payload = payload
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