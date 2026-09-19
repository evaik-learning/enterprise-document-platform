namespace Edp.DigitalSignature.Application.Contracts;

using Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Command to create a new signing request.
/// </summary>
public class CreateSigningRequestCommand
{
    public Guid WorkflowInstanceId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentVersionId { get; set; }
    public string DocumentHash { get; set; } = string.Empty;
    public SigningMode SigningMode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Provider { get; set; } = "LocalDemo";
    public List<SignerInputDto> Signers { get; set; } = new();
    public List<SignatureFieldInputDto> SignatureFields { get; set; } = new();
    public Guid CorrelationId { get; set; }
}

/// <summary>
/// DTO for signer information when creating a request.
/// </summary>
public class SignerInputDto
{
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int SigningOrder { get; set; }
    public bool IsRequired { get; set; }
}

/// <summary>
/// DTO for signature field information when creating a request.
/// </summary>
public class SignatureFieldInputDto
{
    public Guid SignerId { get; set; }
    public int DocumentPage { get; set; }
    public FieldType FieldType { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public bool Required { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Request to sign a document.
/// </summary>
public class SignRequest
{
    public byte[]? SignatureBytes { get; set; }
    public string? SignatureValue { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Summary DTO for signing request.
/// </summary>
public class SigningRequestDto
{
    public Guid SigningRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public SigningRequestStatus Status { get; set; }
    public SigningMode SigningMode { get; set; }
    public int SignerCount { get; set; }
    public int SignedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Detailed DTO for signing request.
/// </summary>
public class SigningRequestDetailDto
{
    public Guid SigningRequestId { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public SigningRequestStatus Status { get; set; }
    public SigningMode SigningMode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<SignerDto> Signers { get; set; } = new();
    public List<SignatureFieldDto> SignatureFields { get; set; } = new();
    public List<AuditEntryDto> AuditTrail { get; set; } = new();
}

/// <summary>
/// DTO for a signer.
/// </summary>
public class SignerDto
{
    public Guid SignerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public SignerStatus Status { get; set; }
    public int SigningOrder { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public string? DeclineReason { get; set; }
}

/// <summary>
/// DTO for a signature field.
/// </summary>
public class SignatureFieldDto
{
    public Guid SignatureFieldId { get; set; }
    public Guid SignerId { get; set; }
    public int DocumentPage { get; set; }
    public FieldType FieldType { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public bool Required { get; set; }
    public string? Label { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// DTO for an audit entry.
/// </summary>
public class AuditEntryDto
{
    public Guid SignatureActionId { get; set; }
    public Guid SignerId { get; set; }
    public string SignerEmail { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Filter for listing signing requests.
/// </summary>
public class ListSigningRequestsFilter
{
    public SigningRequestStatus? Status { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public string? SignerEmail { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; } = "CreatedAt:desc";
}
