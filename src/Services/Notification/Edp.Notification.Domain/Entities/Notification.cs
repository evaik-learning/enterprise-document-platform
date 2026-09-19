namespace Edp.Notification.Domain.Entities;

public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Teams = 3
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Read = 3,
    Failed = 4,
    Cancelled = 5
}

public sealed class Notification
{
    private Notification() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public Guid SourceEventId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    public static Notification Create(
        Guid organizationId,
        Guid userId,
        string notificationType,
        string subject,
        string body,
        Guid sourceEventId,
        string correlationId,
        NotificationChannel channel = NotificationChannel.InApp)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.", nameof(organizationId));
        if (userId == Guid.Empty) throw new ArgumentException("Recipient is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new Notification
        {
            OrganizationId = organizationId,
            UserId = userId,
            NotificationType = notificationType.Trim(),
            Subject = subject.Trim(),
            Body = body,
            Channel = channel,
            Status = NotificationStatus.Sent,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SourceEventId = sourceEventId,
            CorrelationId = correlationId.Trim()
        };
    }

    public void MarkRead()
    {
        if (Status is NotificationStatus.Cancelled or NotificationStatus.Failed) return;
        Status = NotificationStatus.Read;
        ReadAtUtc = DateTimeOffset.UtcNow;
    }
}
