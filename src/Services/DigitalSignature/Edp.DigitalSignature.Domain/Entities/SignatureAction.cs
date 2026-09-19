namespace Edp.DigitalSignature.Domain.Entities;

using Edp.SharedKernel.Entities;
using Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Represents an audit entry for a signature action.
/// </summary>
public class SignatureAction : BaseEntity<Guid>
{
    /// <summary>
    /// Unique identifier for this action.
    /// </summary>
    public Guid SignatureActionId { get; protected set; }

    /// <summary>
    /// Reference to the signing request.
    /// </summary>
    public Guid SigningRequestId { get; protected set; }

    /// <summary>
    /// Reference to the signer who performed the action.
    /// </summary>
    public Guid SignerId { get; protected set; }

    /// <summary>
    /// Type of action performed.
    /// </summary>
    public SignatureActionType ActionType { get; protected set; }

    /// <summary>
    /// Date and time when the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; protected set; }

    /// <summary>
    /// IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; protected set; }

    /// <summary>
    /// User agent string from the client.
    /// </summary>
    public string? UserAgent { get; protected set; }

    /// <summary>
    /// Encrypted signature data (if applicable).
    /// </summary>
    public byte[]? SignatureData { get; protected set; }

    protected SignatureAction()
    {
        Id = Guid.NewGuid();
        SignatureActionId = Id;
    }

    /// <summary>
    /// Creates a new signature action.
    /// </summary>
    public static SignatureAction Create(
        Guid signingRequestId,
        Guid signerId,
        SignatureActionType actionType,
        string? ipAddress = null,
        string? userAgent = null,
        byte[]? signatureData = null)
    {
        return new SignatureAction
        {
            Id = Guid.NewGuid(),
            SignatureActionId = Guid.NewGuid(),
            SigningRequestId = signingRequestId,
            SignerId = signerId,
            ActionType = actionType,
            OccurredAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SignatureData = signatureData
        };
    }
}
