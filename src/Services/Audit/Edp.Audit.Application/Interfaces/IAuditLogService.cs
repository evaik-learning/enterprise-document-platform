using Edp.Audit.Application.Commands;
using Edp.Audit.Domain.Entities;

namespace Edp.Audit.Application.Interfaces;

public interface IAuditLogService
{
    Task<AuditLog> RecordAsync(RecordAuditEventCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
