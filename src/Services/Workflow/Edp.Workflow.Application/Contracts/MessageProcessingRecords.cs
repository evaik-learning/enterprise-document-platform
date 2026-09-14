namespace Edp.Workflow.Application.Contracts;

public sealed class IdempotencyRecord
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public int ResponseStatusCode { get; private set; }
    public string ResponseBody { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    private IdempotencyRecord() { }

    public IdempotencyRecord(Guid organizationId, string key, string requestHash, int responseStatusCode, string responseBody, DateTimeOffset expiresAtUtc)
    {
        OrganizationId = organizationId;
        Key = key;
        RequestHash = requestHash;
        ResponseStatusCode = responseStatusCode;
        ResponseBody = responseBody;
        ExpiresAtUtc = expiresAtUtc;
    }
}

public sealed class InboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string MessageId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public Guid? OrganizationId { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public string? Error { get; private set; }

    private InboxMessage() { }

    public InboxMessage(string messageId, string eventType, Guid? organizationId)
    {
        MessageId = messageId;
        EventType = eventType;
        OrganizationId = organizationId;
    }

    public void MarkProcessed() => ProcessedAtUtc = DateTimeOffset.UtcNow;
    public void MarkFailed(string error) => Error = error;
}
