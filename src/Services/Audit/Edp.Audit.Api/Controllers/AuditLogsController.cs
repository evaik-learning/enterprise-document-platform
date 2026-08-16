using Edp.Audit.Application.Commands;
using Edp.Audit.Application.Interfaces;
using Edp.Audit.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Audit.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet("organization/{organizationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> GetByOrganization(Guid organizationId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetByOrganizationAsync(organizationId, cancellationToken);
        return Ok(logs);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> GetByUser(Guid userId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetByUserAsync(userId, cancellationToken);
        return Ok(logs);
    }

    [HttpPost]
    public async Task<ActionResult<AuditLog>> Record([FromBody] RecordAuditEventCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Action))
        {
            return BadRequest("Action is required.");
        }

        var auditLog = await _auditLogService.RecordAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetByOrganization), new { organizationId = auditLog.OrganizationId }, auditLog);
    }
}
