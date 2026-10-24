namespace Edp.Notification.Domain.Entities;

/// <summary>Per-user, per-organization notification delivery preferences.</summary>
public sealed class NotificationPreference
{
    private NotificationPreference() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; } = true;
    public bool InAppEnabled { get; private set; } = true;
    public bool WorkflowEnabled { get; private set; } = true;
    public bool SignatureEnabled { get; private set; } = true;
    public bool AuditEnabled { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static NotificationPreference CreateDefault(Guid organizationId, Guid userId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization is required.", nameof(organizationId));
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));
        return new NotificationPreference { OrganizationId = organizationId, UserId = userId };
    }

    public void Update(bool emailEnabled, bool inAppEnabled, bool workflowEnabled, bool signatureEnabled, bool auditEnabled)
    {
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        WorkflowEnabled = workflowEnabled;
        SignatureEnabled = signatureEnabled;
        AuditEnabled = auditEnabled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Whether an in-app notification of the given type should be created for this user.</summary>
    public bool AllowsInApp(string notificationType)
    {
        if (!InAppEnabled) return false;
        if (notificationType.StartsWith("Signature", StringComparison.OrdinalIgnoreCase)) return SignatureEnabled;
        if (notificationType.StartsWith("Workflow", StringComparison.OrdinalIgnoreCase)
            || notificationType.StartsWith("Approval", StringComparison.OrdinalIgnoreCase)) return WorkflowEnabled;
        return true;
    }
}
