namespace Edp.DigitalSignature.Application.Interfaces;

using Edp.DigitalSignature.Application.Contracts;

/// <summary>
/// Service for recording and retrieving audit entries for signing actions.
/// </summary>
public interface ISigningAuditService
{
    /// <summary>
    /// Records a signature action in the audit trail.
    /// </summary>
    Task RecordActionAsync(
        Guid signingRequestId,
        Guid signerId,
        string actionType,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit trail for a signing request.
    /// </summary>
    Task<List<AuditEntryDto>> GetAuditTrailAsync(
        Guid signingRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries for a specific signer.
    /// </summary>
    Task<List<AuditEntryDto>> GetSignerAuditTrailAsync(
        Guid signingRequestId,
        Guid signerId,
        CancellationToken cancellationToken = default);
}
