using Edp.Audit.Domain.Entities;

namespace Edp.Audit.Application.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<bool> ExistsForEventAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<AuditLog?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> SearchAsync(Guid organizationId, string? entityType, Guid? entityId, string? action, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default);
}
