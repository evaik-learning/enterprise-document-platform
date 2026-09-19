namespace Edp.DigitalSignature.Domain.Entities;

using Edp.SharedKernel.Entities;
using Edp.DigitalSignature.Domain.Enums;
using Edp.DigitalSignature.Domain.Events;
using Edp.DigitalSignature.Domain.Exceptions;

/// <summary>
/// Aggregate root for signing requests.
/// Represents a request to obtain signatures on a document from one or more signers.
/// </summary>
public class SigningRequest : AuditableEntity<Guid>
{
    /// <summary>
    /// Unique identifier for this signing request.
    /// </summary>
    public Guid SigningRequestId { get; protected set; }

    /// <summary>
    /// Organization ID for tenant isolation.
    /// </summary>
    public Guid OrganizationId { get; protected set; }

    /// <summary>
    /// Reference to the workflow instance this signing request is part of.
    /// </summary>
    public Guid WorkflowInstanceId { get; protected set; }

    /// <summary>
    /// Reference to the document being signed.
    /// </summary>
    public Guid DocumentId { get; protected set; }

    /// <summary>
    /// Reference to the specific version of the document.
    /// </summary>
    public Guid DocumentVersionId { get; protected set; }

    /// <summary>
    /// Current status of the signing request.
    /// </summary>
    public SigningRequestStatus Status { get; protected set; }

    /// <summary>
    /// Determines if signers sign sequentially or in parallel.
    /// </summary>
    public SigningMode SigningMode { get; protected set; }

    /// <summary>
    /// Human-readable title for the signing request.
    /// </summary>
    public string Title { get; protected set; } = string.Empty;

    /// <summary>
    /// Optional message to display to signers.
    /// </summary>
    public string? Message { get; protected set; }

    /// <summary>
    /// Date and time when this signing request expires. Null means no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; protected set; }

    /// <summary>
    /// Date and time when the signing request was activated.
    /// </summary>
    public DateTime? ActivatedAt { get; protected set; }

    /// <summary>
    /// Date and time when the signing request was completed.
    /// </summary>
    public DateTime? CompletedAt { get; protected set; }

    /// <summary>
    /// Date and time when the signing request was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; protected set; }

    /// <summary>
    /// SHA-256 hash of the original document for immutability verification.
    /// </summary>
    public string DocumentHash { get; protected set; } = string.Empty;

    /// <summary>
    /// Name of the signature provider (e.g., "LocalDemo", "DocuSign").
    /// </summary>
    public string Provider { get; protected set; } = string.Empty;

    /// <summary>
    /// ID assigned by the provider for this signing session.
    /// Used for idempotency tracking.
    /// </summary>
    public string? ProviderRequestId { get; protected set; }

    public void SetProviderRequestId(string providerRequestId)
    {
        if (string.IsNullOrWhiteSpace(providerRequestId))
        {
            throw new ArgumentException("Provider request ID is required.", nameof(providerRequestId));
        }

        ProviderRequestId = providerRequestId;
    }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public Guid CorrelationId { get; protected set; }

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Collection of signers on this request.
    /// </summary>
    public ICollection<Signer> Signers { get; protected set; } = new List<Signer>();

    #region Constructors

    protected SigningRequest()
    {
        Id = Guid.NewGuid();
        SigningRequestId = Id;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new signing request.
    /// </summary>
    public static SigningRequest Create(
        Guid organizationId,
        Guid workflowInstanceId,
        Guid documentId,
        Guid documentVersionId,
        SigningMode signingMode,
        string title,
        string? message,
        DateTime? expiresAt,
        string documentHash,
        string provider,
        Guid correlationId)
    {
        var request = new SigningRequest
        {
            Id = Guid.NewGuid(),
            SigningRequestId = Guid.NewGuid(),
            OrganizationId = organizationId,
            WorkflowInstanceId = workflowInstanceId,
            DocumentId = documentId,
            DocumentVersionId = documentVersionId,
            Status = SigningRequestStatus.Draft,
            SigningMode = signingMode,
            Title = title,
            Message = message,
            ExpiresAt = expiresAt,
            DocumentHash = documentHash,
            Provider = provider,
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        request.AddDomainEvent(new SigningRequestCreatedDomainEvent(
            SigningRequestId: request.SigningRequestId,
            OrganizationId: request.OrganizationId,
            WorkflowInstanceId: request.WorkflowInstanceId,
            DocumentId: request.DocumentId,
            DocumentVersionId: request.DocumentVersionId,
            Title: request.Title,
            Message: request.Message,
            SigningMode: request.SigningMode,
            ExpiresAt: request.ExpiresAt,
            DocumentHash: request.DocumentHash,
            CorrelationId: request.CorrelationId,
            CreatedAt: DateTime.UtcNow
        ));

        return request;
    }

    #endregion

    #region State Guard Methods

    /// <summary>
    /// Checks if this request can be activated.
    /// </summary>
    public bool CanActivate() => Status == SigningRequestStatus.Draft;

    /// <summary>
    /// Checks if signatures can be added.
    /// </summary>
    public bool CanSign() =>
        Status == SigningRequestStatus.Pending ||
        Status == SigningRequestStatus.InProgress ||
        Status == SigningRequestStatus.PartiallySigned;

    /// <summary>
    /// Checks if the request can be declined.
    /// </summary>
    public bool CanDecline() =>
        Status == SigningRequestStatus.Pending ||
        Status == SigningRequestStatus.InProgress ||
        Status == SigningRequestStatus.PartiallySigned;

    /// <summary>
    /// Checks if the request can be cancelled.
    /// </summary>
    public bool CanCancel() =>
        Status != SigningRequestStatus.Completed &&
        Status != SigningRequestStatus.Expired &&
        Status != SigningRequestStatus.Cancelled &&
        Status != SigningRequestStatus.Failed;

    /// <summary>
    /// Checks if the request can expire.
    /// </summary>
    public bool CanExpire() =>
        Status != SigningRequestStatus.Completed &&
        Status != SigningRequestStatus.Expired &&
        Status != SigningRequestStatus.Cancelled &&
        Status != SigningRequestStatus.Failed;

    #endregion

    #region State Transition Methods

    /// <summary>
    /// Activates the signing request, transitioning from Draft to Pending.
    /// </summary>
    public void Activate()
    {
        if (!CanActivate())
        {
            throw new InvalidSigningStateException(Status.ToString(), nameof(Activate));
        }

        Status = SigningRequestStatus.Pending;
        ActivatedAt = DateTime.UtcNow;

        AddDomainEvent(new SigningRequestActivatedDomainEvent(
            SigningRequestId: SigningRequestId,
            OrganizationId: OrganizationId,
            ActivatedAt: ActivatedAt.Value,
            CorrelationId: CorrelationId
        ));
    }

    /// <summary>
    /// Starts the signing process (transitions to InProgress if Sequential, or awaiting signers).
    /// </summary>
    public void StartSigning()
    {
        if (!CanSign())
        {
            throw new InvalidSigningStateException(Status.ToString(), nameof(StartSigning));
        }

        Status = SigningRequestStatus.InProgress;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Completes the signing request when all signers have signed.
    /// </summary>
    public void CompleteSigning()
    {
        if (Status != SigningRequestStatus.InProgress && Status != SigningRequestStatus.PartiallySigned)
        {
            throw new InvalidSigningStateException(Status.ToString(), nameof(CompleteSigning));
        }

        Status = SigningRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ModifiedAt = DateTimeOffset.UtcNow;

        var totalSigners = Signers.Count;
        var signedCount = Signers.Count(s => s.Status == SignerStatus.Signed);

        AddDomainEvent(new SigningRequestCompletedDomainEvent(
            SigningRequestId: SigningRequestId,
            OrganizationId: OrganizationId,
            WorkflowInstanceId: WorkflowInstanceId,
            DocumentId: DocumentId,
            CompletedAt: CompletedAt.Value,
            TotalSigners: totalSigners,
            SignersCount: signedCount,
            CorrelationId: CorrelationId
        ));
    }

    public void RecordSignedDocument(string signedDocumentPath, string documentHash)
    {
        if (Status != SigningRequestStatus.Completed)
        {
            throw new InvalidSigningStateException(Status.ToString(), nameof(RecordSignedDocument));
        }

        AddDomainEvent(new SignedDocumentCreatedDomainEvent(
            SigningRequestId,
            OrganizationId,
            DocumentId,
            signedDocumentPath,
            documentHash,
            DateTime.UtcNow,
            CorrelationId));
    }

    /// <summary>
    /// Cancels the signing request.
    /// </summary>
    public void Cancel()
    {
        if (!CanCancel())
        {
            throw new InvalidSigningStateException(Status.ToString(), nameof(Cancel));
        }

        Status = SigningRequestStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        ModifiedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new SigningRequestCancelledDomainEvent(
            SigningRequestId: SigningRequestId,
            OrganizationId: OrganizationId,
            CancelledAt: CancelledAt.Value,
            CorrelationId: CorrelationId
        ));
    }

    /// <summary>
    /// Marks this request as declined (when a signer declines).
    /// </summary>
    public void MarkAsDeclined()
    {
        if (!CanDecline())
        {
            throw new InvalidSigningStateException(Status.ToString(), "MarkAsDeclined");
        }

        Status = SigningRequestStatus.Declined;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks this request as expired.
    /// </summary>
    public void Expire()
    {
        if (!CanExpire())
        {
            throw new InvalidSigningStateException(Status.ToString(), nameof(Expire));
        }

        Status = SigningRequestStatus.Expired;
        ModifiedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new SigningRequestExpiredDomainEvent(
            SigningRequestId: SigningRequestId,
            OrganizationId: OrganizationId,
            ExpiredAt: DateTime.UtcNow,
            CorrelationId: CorrelationId
        ));
    }

    #endregion
}
