namespace Edp.DigitalSignature.Domain.Entities;

using Edp.SharedKernel.Entities;
using Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Represents an individual signer in a signing request.
/// </summary>
public class Signer : BaseEntity<Guid>
{
    /// <summary>
    /// Unique identifier for this signer.
    /// </summary>
    public Guid SignerId { get; protected set; }

    /// <summary>
    /// Reference to the signing request this signer belongs to.
    /// </summary>
    public Guid SigningRequestId { get; protected set; }

    /// <summary>
    /// User ID if this is an internal user. Null for external signers.
    /// </summary>
    public Guid? UserId { get; protected set; }

    /// <summary>
    /// Email address of the signer.
    /// </summary>
    public string Email { get; protected set; } = string.Empty;

    /// <summary>
    /// Display name of the signer.
    /// </summary>
    public string DisplayName { get; protected set; } = string.Empty;

    /// <summary>
    /// Role description (e.g., "Customer", "Approver", "Witness").
    /// </summary>
    public string Role { get; protected set; } = string.Empty;

    /// <summary>
    /// Order in which this signer should sign (for sequential signing mode).
    /// </summary>
    public int SigningOrder { get; protected set; }

    /// <summary>
    /// Current status of this signer.
    /// </summary>
    public SignerStatus Status { get; protected set; }

    /// <summary>
    /// Whether this signer's signature is required.
    /// </summary>
    public bool IsRequired { get; protected set; }

    /// <summary>
    /// Date and time when the signer was invited.
    /// </summary>
    public DateTime? InvitedAt { get; protected set; }

    /// <summary>
    /// Date and time when the signer viewed the document.
    /// </summary>
    public DateTime? ViewedAt { get; protected set; }

    /// <summary>
    /// Date and time when the signer signed the document.
    /// </summary>
    public DateTime? SignedAt { get; protected set; }

    /// <summary>
    /// Date and time when the signer declined to sign.
    /// </summary>
    public DateTime? DeclinedAt { get; protected set; }

    /// <summary>
    /// Reason provided if the signer declined.
    /// </summary>
    public string? DeclineReason { get; protected set; }

    /// <summary>
    /// ID assigned by the signature provider for this signer.
    /// </summary>
    public string? ProviderSignerId { get; protected set; }

    /// <summary>
    /// Date and time of the last reminder sent to this signer.
    /// </summary>
    public DateTime? LastReminderSentAt { get; protected set; }

    /// <summary>
    /// Number of reminders sent to this signer.
    /// </summary>
    public int ReminderCount { get; protected set; }

    /// <summary>
    /// Navigation property to the signing request.
    /// </summary>
    public SigningRequest? SigningRequest { get; protected set; }

    protected Signer()
    {
        Id = Guid.NewGuid();
        SignerId = Id;
    }

    /// <summary>
    /// Creates a new signer.
    /// </summary>
    public static Signer Create(
        Guid signingRequestId,
        Guid? userId,
        string email,
        string displayName,
        string role,
        int signingOrder,
        bool isRequired)
    {
        return new Signer
        {
            Id = Guid.NewGuid(),
            SignerId = Guid.NewGuid(),
            SigningRequestId = signingRequestId,
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            Role = role,
            SigningOrder = signingOrder,
            Status = SignerStatus.Pending,
            IsRequired = isRequired,
            ReminderCount = 0
        };
    }

    /// <summary>
    /// Marks this signer as invited.
    /// </summary>
    public void MarkAsInvited()
    {
        Status = SignerStatus.Invited;
        InvitedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this signer as having viewed the document.
    /// </summary>
    public void MarkAsViewed()
    {
        Status = SignerStatus.Viewed;
        ViewedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this signer as ready to sign.
    /// </summary>
    public void MarkAsReadyToSign()
    {
        Status = SignerStatus.ReadyToSign;
    }

    /// <summary>
    /// Marks this signer as signed.
    /// </summary>
    public void MarkAsSigned(string? providerSignerId = null)
    {
        Status = SignerStatus.Signed;
        SignedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(providerSignerId))
        {
            ProviderSignerId = providerSignerId;
        }
    }

    /// <summary>
    /// Marks this signer as declined.
    /// </summary>
    public void MarkAsDeclined(string? reason = null)
    {
        Status = SignerStatus.Declined;
        DeclinedAt = DateTime.UtcNow;
        DeclineReason = reason;
    }

    /// <summary>
    /// Marks this signer as expired.
    /// </summary>
    public void MarkAsExpired()
    {
        Status = SignerStatus.Expired;
    }

    /// <summary>
    /// Records a reminder sent to this signer.
    /// </summary>
    public void RecordReminder()
    {
        LastReminderSentAt = DateTime.UtcNow;
        ReminderCount++;
    }
}
