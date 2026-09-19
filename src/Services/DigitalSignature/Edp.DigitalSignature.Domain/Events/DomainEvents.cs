namespace Edp.DigitalSignature.Domain.Events;

using Edp.SharedKernel.Domain;
using Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Raised when a signing request is created.
/// </summary>
public record SigningRequestCreatedDomainEvent(
    Guid SigningRequestId,
    Guid OrganizationId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid DocumentVersionId,
    string Title,
    string? Message,
    SigningMode SigningMode,
    DateTime? ExpiresAt,
    string DocumentHash,
    Guid CorrelationId,
    DateTime CreatedAt
) : DomainEvent;

/// <summary>
/// Raised when a signing request is activated.
/// </summary>
public record SigningRequestActivatedDomainEvent(
    Guid SigningRequestId,
    Guid OrganizationId,
    DateTime ActivatedAt,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signer is invited to sign.
/// </summary>
public record SignerInvitedDomainEvent(
    Guid SigningRequestId,
    Guid SignerId,
    Guid OrganizationId,
    string Email,
    string DisplayName,
    DateTime InvitedAt,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signer views the document.
/// </summary>
public record SignerViewedDocumentDomainEvent(
    Guid SigningRequestId,
    Guid SignerId,
    Guid OrganizationId,
    DateTime ViewedAt,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signer signs the document.
/// </summary>
public record SignerSignedDomainEvent(
    Guid SigningRequestId,
    Guid SignerId,
    Guid OrganizationId,
    string Email,
    DateTime SignedAt,
    string? ProviderSignatureId,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signer declines to sign.
/// </summary>
public record SignerDeclinedDomainEvent(
    Guid SigningRequestId,
    Guid SignerId,
    Guid OrganizationId,
    string Email,
    string? Reason,
    DateTime DeclinedAt,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signing request expires.
/// </summary>
public record SigningRequestExpiredDomainEvent(
    Guid SigningRequestId,
    Guid OrganizationId,
    DateTime ExpiredAt,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signing request is cancelled.
/// </summary>
public record SigningRequestCancelledDomainEvent(
    Guid SigningRequestId,
    Guid OrganizationId,
    DateTime CancelledAt,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signing request is completed.
/// </summary>
public record SigningRequestCompletedDomainEvent(
    Guid SigningRequestId,
    Guid OrganizationId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    DateTime CompletedAt,
    int TotalSigners,
    int SignersCount,
    Guid CorrelationId
) : DomainEvent;

/// <summary>
/// Raised when a signed document is created.
/// </summary>
public record SignedDocumentCreatedDomainEvent(
    Guid SigningRequestId,
    Guid OrganizationId,
    Guid DocumentId,
    string SignedDocumentPath,
    string DocumentHash,
    DateTime CreatedAt,
    Guid CorrelationId
) : DomainEvent;
