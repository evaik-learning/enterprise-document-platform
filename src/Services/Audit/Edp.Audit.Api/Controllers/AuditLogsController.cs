using Edp.Audit.Application.Commands;
using Edp.Audit.Application.Interfaces;
using Edp.Audit.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Edp.Shared.Security.CurrentUser;
using Edp.Shared.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Edp.Audit.Api.Controllers;

[ApiController]
[Authorize(Policy = "audit.read")]
[Route("api/v1/[controller]")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;

    public AuditLogsController(IAuditLogService auditLogService, ICurrentOrganization currentOrganization, ICurrentUser currentUser)
    {
        _auditLogService = auditLogService;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
    }

    [HttpGet("organization/{organizationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> GetByOrganization(Guid organizationId, CancellationToken cancellationToken)
    {
        RequireOrganization(organizationId);
        var logs = await _auditLogService.GetByOrganizationAsync(organizationId, cancellationToken);
        return Ok(logs);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> GetByUser(Guid userId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetByUserAsync(RequireOrganization(), userId, cancellationToken);
        return Ok(logs);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> Search([FromQuery] Guid organizationId, [FromQuery] string? entityType, [FromQuery] Guid? entityId, [FromQuery] string? action, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        RequireOrganization(organizationId);
        var logs = await _auditLogService.SearchAsync(organizationId, entityType, entityId, action, from, to, page, pageSize, cancellationToken);
        return Ok(logs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLog>> Get(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        RequireOrganization(organizationId);
        var log = await _auditLogService.GetByIdAsync(organizationId, id, cancellationToken);
        return log is null ? NotFound() : Ok(log);
    }

    [HttpPost]
    public async Task<ActionResult<AuditLog>> Record([FromBody] RecordAuditEventCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Action))
        {
            return BadRequest("Action is required.");
        }

        var auditLog = await _auditLogService.RecordAsync(command with
        {
            OrganizationId = RequireOrganization(),
            UserId = _currentUser.UserId == Guid.Empty ? command.UserId : _currentUser.UserId
        }, cancellationToken);
        return CreatedAtAction(nameof(GetByOrganization), new { organizationId = auditLog.OrganizationId }, auditLog);
    }

    private Guid RequireOrganization(Guid? requestedOrganizationId = null)
    {
        var organizationId = _currentOrganization.OrganizationId
            ?? throw new ForbiddenProblemDetailsException("An organization context is required.", "AUDIT_ORGANIZATION_REQUIRED");
        if (requestedOrganizationId.HasValue && requestedOrganizationId.Value != organizationId)
            throw new ForbiddenProblemDetailsException("The requested organization is not accessible.", "AUDIT_ORGANIZATION_FORBIDDEN");
        return organizationId;
    }
}
