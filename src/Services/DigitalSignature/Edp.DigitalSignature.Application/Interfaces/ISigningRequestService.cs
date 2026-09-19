namespace Edp.DigitalSignature.Application.Interfaces;

using Edp.DigitalSignature.Application.Contracts;

/// <summary>
/// Service for managing signing requests and the signing workflow.
/// </summary>
public interface ISigningRequestService
{
    /// <summary>
    /// Creates a new signing request.
    /// </summary>
    Task<SigningRequestDto> CreateSigningRequestAsync(
        CreateSigningRequestCommand request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a signing request, transitioning it from Draft to Pending.
    /// </summary>
    Task ActivateSigningRequestAsync(
        Guid signingRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves detailed information about a signing request.
    /// </summary>
    Task<SigningRequestDetailDto> GetSigningRequestAsync(
        Guid signingRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists signing requests with optional filtering and paging.
    /// </summary>
    Task<PagedResult<SigningRequestDto>> ListSigningRequestsAsync(
        ListSigningRequestsFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a signature from a signer.
    /// </summary>
    Task SignAsync(
        Guid signingRequestId,
        Guid signerId,
        SignRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a signer's decline to sign.
    /// </summary>
    Task DeclineAsync(
        Guid signingRequestId,
        Guid signerId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a signing request.
    /// </summary>
    Task CancelAsync(
        Guid signingRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends (or resends) an invitation to a signer.
    /// </summary>
    Task ResendInvitationAsync(
        Guid signingRequestId,
        Guid signerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the audit trail for a signing request.
    /// </summary>
    Task<List<AuditEntryDto>> GetAuditTrailAsync(
        Guid signingRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Background job to process expired signing requests.
    /// </summary>
    Task ProcessExpiredRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Background job to send reminders to pending signers.
    /// </summary>
    Task ProcessRemindersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic paged result wrapper.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
