namespace Edp.DigitalSignature.Api.Controllers;

using Edp.DigitalSignature.Application.Contracts;
using Edp.DigitalSignature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API endpoints for managing digital signing requests.
/// Handles creation, activation, signing, tracking, and audit of signing workflows.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class SigningRequestsController : ControllerBase
{
    private readonly ISigningRequestService _signingRequestService;
    private readonly ILogger<SigningRequestsController> _logger;

    public SigningRequestsController(
        ISigningRequestService signingRequestService,
        ILogger<SigningRequestsController> logger)
    {
        _signingRequestService = signingRequestService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new signing request.
    /// </summary>
    /// <param name="command">Signing request creation command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created signing request with ID</returns>
    /// <response code="201">Signing request created successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="409">Conflict - document or workflow not found</response>
    [HttpPost]
    [Authorize(Policy = "SigningManager")]
    [ProducesResponseType(typeof(SigningRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SigningRequestDto>> CreateAsync(
        [FromBody] CreateSigningRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating signing request for workflow {WorkflowInstanceId}, document {DocumentId}",
            command.WorkflowInstanceId, command.DocumentId);

        var result = await _signingRequestService.CreateSigningRequestAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetAsync), new { id = result.SigningRequestId }, result);
    }

    /// <summary>
    /// Gets a signing request by ID.
    /// </summary>
    /// <param name="id">Signing request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signing request details</returns>
    /// <response code="200">Signing request found</response>
    /// <response code="404">Signing request not found</response>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "signing.read")]
    [ProducesResponseType(typeof(SigningRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SigningRequestDetailDto>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting signing request {SigningRequestId}", id);

        var result = await _signingRequestService.GetSigningRequestAsync(id, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = $"Signing request {id} not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lists signing requests with optional filtering and pagination.
    /// </summary>
    /// <param name="filter">Filter and pagination options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of signing requests</returns>
    /// <response code="200">Signing requests retrieved</response>
    /// <response code="400">Invalid filter parameters</response>
    [HttpGet]
    [Authorize(Policy = "signing.read")]
    [ProducesResponseType(typeof(PagedResult<SigningRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SigningRequestDto>>> ListAsync(
        [FromQuery] ListSigningRequestsFilter filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing signing requests with filter {Filter}", filter);

        var result = await _signingRequestService.ListSigningRequestsAsync(filter, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Activates a signing request, sending invitations to all signers.
    /// </summary>
    /// <param name="id">Signing request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Status code 204 No Content</returns>
    /// <response code="204">Signing request activated successfully</response>
    /// <response code="404">Signing request not found</response>
    /// <response code="409">Invalid state for activation</response>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "SigningManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating signing request {SigningRequestId}", id);

        await _signingRequestService.ActivateSigningRequestAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Records a signature for a specific signer on their signing request.
    /// </summary>
    /// <param name="id">Signing request ID</param>
    /// <param name="signerId">Signer ID</param>
    /// <param name="request">Signature data (bytes or value)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Status code 204 No Content</returns>
    /// <response code="204">Signature recorded successfully</response>
    /// <response code="404">Signing request or signer not found</response>
    /// <response code="409">Invalid state for signing</response>
    [HttpPost("{id:guid}/sign/{signerId:guid}")]
    [Authorize(Policy = "Signer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignAsync(
        Guid id,
        Guid signerId,
        [FromBody] SignRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Recording signature for signer {SignerId} on signing request {SigningRequestId}",
            signerId, id);

        await _signingRequestService.SignAsync(id, signerId, request, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Records a signer's refusal to sign.
    /// </summary>
    /// <param name="id">Signing request ID</param>
    /// <param name="signerId">Signer ID</param>
    /// <param name="reason">Optional decline reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Status code 204 No Content</returns>
    /// <response code="204">Decline recorded successfully</response>
    /// <response code="404">Signing request or signer not found</response>
    /// <response code="409">Invalid state for decline</response>
    [HttpPost("{id:guid}/decline/{signerId:guid}")]
    [Authorize(Policy = "Signer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeclineAsync(
        Guid id,
        Guid signerId,
        [FromQuery] string? reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Recording decline for signer {SignerId} on signing request {SigningRequestId}",
            signerId, id);

        await _signingRequestService.DeclineAsync(id, signerId, reason, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Cancels a signing request, preventing further signatures.
    /// </summary>
    /// <param name="id">Signing request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Status code 204 No Content</returns>
    /// <response code="204">Signing request cancelled successfully</response>
    /// <response code="404">Signing request not found</response>
    /// <response code="409">Invalid state for cancellation</response>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "SigningManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling signing request {SigningRequestId}", id);

        await _signingRequestService.CancelAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Resends the signing invitation to a specific signer.
    /// </summary>
    /// <param name="id">Signing request ID</param>
    /// <param name="signerId">Signer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Status code 204 No Content</returns>
    /// <response code="204">Reminder sent successfully</response>
    /// <response code="404">Signing request or signer not found</response>
    /// <response code="409">Too many reminders or invalid state</response>
    [HttpPost("{id:guid}/resend-invitation/{signerId:guid}")]
    [Authorize(Policy = "SigningManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendInvitationAsync(
        Guid id,
        Guid signerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Resending invitation to signer {SignerId} on signing request {SigningRequestId}",
            signerId, id);

        await _signingRequestService.ResendInvitationAsync(id, signerId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Gets the audit trail (all actions taken on a signing request).
    /// </summary>
    /// <param name="id">Signing request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries</returns>
    /// <response code="200">Audit trail retrieved</response>
    /// <response code="404">Signing request not found</response>
    [HttpGet("{id:guid}/audit-trail")]
    [Authorize(Policy = "Auditor")]
    [ProducesResponseType(typeof(List<AuditEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AuditEntryDto>>> GetAuditTrailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting audit trail for signing request {SigningRequestId}", id);

        var result = await _signingRequestService.GetAuditTrailAsync(id, cancellationToken);

        if (result == null || result.Count == 0)
        {
            return NotFound(new { message = $"Signing request {id} not found or has no audit trail" });
        }

        return Ok(result);
    }
}
