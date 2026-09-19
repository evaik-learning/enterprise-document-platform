using Edp.Audit.Application.Commands;
using Edp.Audit.Domain.Entities;

namespace Edp.Audit.Application.Interfaces;

public interface IAuditLogService
{
    Task<AuditLog> RecordAsync(RecordAuditEventCommand command, CancellationToken cancellationToken = default);
    Task<bool> ExistsForEventAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<AuditLog?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> SearchAsync(Guid organizationId, string? entityType, Guid? entityId, string? action, string? correlationId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default);
}
