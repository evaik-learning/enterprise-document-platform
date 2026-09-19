namespace Edp.DigitalSignature.Application.Interfaces;

/// <summary>
/// Abstraction for signature providers (DocuSign, Adobe Sign, LocalDemo, etc.).
/// </summary>
public interface ISignatureProvider
{
    /// <summary>
    /// Creates a new signing envelope/session with the provider.
    /// </summary>
    Task<SignatureEnvelopeResult> CreateEnvelopeAsync(
        SignatureEnvelopeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a signing session from the provider.
    /// </summary>
    Task<SignatureStatusResult> GetStatusAsync(
        string providerRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a signature action for a signer.
    /// </summary>
    Task<SignatureActionResult> SignAsync(
        string providerRequestId,
        string signerId,
        SignatureActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an in-progress signing session.
    /// </summary>
    Task CancelAsync(
        string providerRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the completed/signed document from the provider.
    /// </summary>
    Task<byte[]> DownloadCompletedDocumentAsync(
        string providerRequestId,
        CancellationToken cancellationToken = default);
}

public interface ISignatureProviderResolver
{
    ISignatureProvider Resolve(string providerName);
}

public interface IDocumentContentStore
{
    Task<Stream?> DownloadOriginalAsync(
        Guid organizationId,
        Guid documentId,
        Guid documentVersionId,
        CancellationToken cancellationToken = default);

    Task<(string Path, string Hash)> SaveSignedAsync(
        Guid organizationId,
        Guid documentId,
        Guid documentVersionId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a new signing envelope.
/// </summary>
public class SignatureEnvelopeRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public byte[] DocumentBytes { get; set; } = [];
    public List<SignerEnvelopeInfo> Signers { get; set; } = new();
    public List<SignatureFieldInfo> Fields { get; set; } = new();
}

/// <summary>
/// Information about a signer for envelope creation.
/// </summary>
public class SignerEnvelopeInfo
{
    public string SignerId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SigningOrder { get; set; }
}

/// <summary>
/// Information about a signature field.
/// </summary>
public class SignatureFieldInfo
{
    public string SignerId { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public bool Required { get; set; }
}

/// <summary>
/// Result from creating a signing envelope.
/// </summary>
public class SignatureEnvelopeResult
{
    public string ProviderRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> SignerInvitationUrls { get; set; } = new();
}

/// <summary>
/// Request to record a signature action.
/// </summary>
public class SignatureActionRequest
{
    public byte[]? SignatureBytes { get; set; }
    public string? SignatureValue { get; set; }
    public DateTime SignedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Result from a signature action.
/// </summary>
public class SignatureActionResult
{
    public bool Success { get; set; }
    public string SignerId { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Status of a signing session from the provider.
/// </summary>
public class SignatureStatusResult
{
    public string ProviderRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<SignerStatusInfo> SignersStatus { get; set; } = new();
}

/// <summary>
/// Status of an individual signer.
/// </summary>
public class SignerStatusInfo
{
    public string SignerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SignedAt { get; set; }
}
