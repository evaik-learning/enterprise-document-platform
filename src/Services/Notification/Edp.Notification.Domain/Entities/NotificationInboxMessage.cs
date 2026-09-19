namespace Edp.Notification.Domain.Entities;

public sealed class NotificationInboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId { get; private set; }
    public string MessageId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public string? Error { get; private set; }

    private NotificationInboxMessage() { }

    public NotificationInboxMessage(
        Guid organizationId,
        string messageId,
        string eventType,
        string payload)
    {
        OrganizationId = organizationId;
        MessageId = messageId;
        EventType = eventType;
        Payload = payload;
    }

    public void MarkProcessed() => ProcessedAtUtc = DateTimeOffset.UtcNow;
    public void MarkFailed(string error) => Error = error;
}
