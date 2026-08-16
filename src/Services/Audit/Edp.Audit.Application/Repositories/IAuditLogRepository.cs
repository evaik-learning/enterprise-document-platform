using Edp.Audit.Domain.Entities;

namespace Edp.Audit.Application.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
