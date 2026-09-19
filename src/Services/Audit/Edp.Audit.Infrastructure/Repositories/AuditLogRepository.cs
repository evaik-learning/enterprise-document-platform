using Edp.Audit.Application.Repositories;
using Edp.Audit.Domain.Entities;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Audit.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly EdpDbContext _dbContext;

    public AuditLogRepository(EdpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public Task<bool> ExistsForEventAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken = default) =>
        _dbContext.AuditLogs.AnyAsync(x => x.OrganizationId == organizationId && x.EventId == eventId, cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.UserId == userId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public Task<AuditLog?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> SearchAsync(Guid organizationId, string? entityType, Guid? entityId, string? action, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
        if (entityId.HasValue) query = query.Where(x => x.EntityId == entityId.Value);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (from.HasValue) query = query.Where(x => x.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(x => x.Timestamp <= to.Value);
        return await query.OrderByDescending(x => x.Timestamp).Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync(cancellationToken);
    }
}
