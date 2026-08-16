using Edp.Audit.Application.Repositories;
using Edp.Audit.Domain.Entities;
using Edp.Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Audit.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _dbContext;

    public AuditLogRepository(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public async Task<IReadOnlyList<AuditLog>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
