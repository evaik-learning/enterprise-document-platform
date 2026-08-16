using Edp.Audit.Application.Commands;
using Edp.Audit.Application.Interfaces;
using Edp.Audit.Application.Repositories;
using Edp.Audit.Domain.Entities;

namespace Edp.Audit.Application.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<AuditLog> RecordAsync(RecordAuditEventCommand command, CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.Create(
            Guid.NewGuid(),
            command.OrganizationId,
            command.UserId,
            command.Action,
            command.EntityType,
            command.EntityId,
            command.CorrelationId,
            command.IpAddress,
            command.Metadata);

        return await _repository.AddAsync(auditLog, cancellationToken);
    }

    public Task<IReadOnlyList<AuditLog>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _repository.GetByOrganizationIdAsync(organizationId, cancellationToken);
    }

    public Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _repository.GetByUserIdAsync(userId, cancellationToken);
    }
}
