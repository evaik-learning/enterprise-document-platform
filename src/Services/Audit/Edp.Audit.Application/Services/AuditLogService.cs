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
            command.Metadata,
            command.EventId);

        return await _repository.AddAsync(auditLog, cancellationToken);
    }

    public Task<bool> ExistsForEventAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken = default) =>
        _repository.ExistsForEventAsync(organizationId, eventId, cancellationToken);

    public Task<IReadOnlyList<AuditLog>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _repository.GetByOrganizationIdAsync(organizationId, cancellationToken);
    }

    public Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _repository.GetByUserIdAsync(organizationId, userId, cancellationToken);
    }

    public Task<AuditLog?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default) => _repository.GetByIdAsync(organizationId, id, cancellationToken);

    public Task<IReadOnlyList<AuditLog>> SearchAsync(Guid organizationId, string? entityType, Guid? entityId, string? action, string? correlationId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default) =>
        _repository.SearchAsync(organizationId, entityType, entityId, action, correlationId, from, to, page, pageSize, cancellationToken);
}
