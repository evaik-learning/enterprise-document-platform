namespace Edp.DigitalSignature.Domain.Entities;

using Edp.SharedKernel.Entities;
using Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Tracks transactions with the signature provider for idempotency and audit purposes.
/// </summary>
public class SigningProviderTransaction : BaseEntity<Guid>
{
    /// <summary>
    /// Unique identifier for this transaction record.
    /// </summary>
    public Guid ProviderTransactionId { get; protected set; }

    /// <summary>
    /// Reference to the signing request.
    /// </summary>
    public Guid SigningRequestId { get; protected set; }

    /// <summary>
    /// Name of the provider (e.g., "LocalDemo", "DocuSign").
    /// </summary>
    public string Provider { get; protected set; } = string.Empty;

    /// <summary>
    /// ID assigned by the provider. Used for idempotency key.
    /// </summary>
    public string ProviderRequestId { get; protected set; } = string.Empty;

    /// <summary>
    /// Type of request sent to the provider.
    /// </summary>
    public ProviderRequestType RequestType { get; protected set; }

    /// <summary>
    /// JSON payload of the request sent to the provider.
    /// </summary>
    public string RequestPayload { get; protected set; } = string.Empty;

    /// <summary>
    /// JSON payload of the response received from the provider.
    /// </summary>
    public string? ResponsePayload { get; protected set; }

    /// <summary>
    /// Current status of this transaction.
    /// </summary>
    public ProviderTransactionStatus Status { get; protected set; }

    /// <summary>
    /// Number of attempts made for this transaction.
    /// </summary>
    public int AttemptCount { get; protected set; }

    /// <summary>
    /// Date and time of the last attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; protected set; }

    /// <summary>
    /// Date and time when this transaction record was created.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    protected SigningProviderTransaction()
    {
        Id = Guid.NewGuid();
        ProviderTransactionId = Id;
    }

    /// <summary>
    /// Creates a new provider transaction record.
    /// </summary>
    public static SigningProviderTransaction Create(
        Guid signingRequestId,
        string provider,
        string providerRequestId,
        ProviderRequestType requestType,
        string requestPayload)
    {
        return new SigningProviderTransaction
        {
            Id = Guid.NewGuid(),
            ProviderTransactionId = Guid.NewGuid(),
            SigningRequestId = signingRequestId,
            Provider = provider,
            ProviderRequestId = providerRequestId,
            RequestType = requestType,
            RequestPayload = requestPayload,
            Status = ProviderTransactionStatus.Pending,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Records a successful response from the provider.
    /// </summary>
    public void RecordSuccess(string responsePayload)
    {
        ResponsePayload = responsePayload;
        Status = ProviderTransactionStatus.Success;
        LastAttemptAt = DateTime.UtcNow;
        AttemptCount++;
    }

    /// <summary>
    /// Records a failed attempt. Sets status to Failed or Retrying based on retry configuration.
    /// </summary>
    public void RecordFailure(string errorMessage, bool willRetry)
    {
        ResponsePayload = errorMessage;
        Status = willRetry ? ProviderTransactionStatus.Retrying : ProviderTransactionStatus.Failed;
        LastAttemptAt = DateTime.UtcNow;
        AttemptCount++;
    }
}
