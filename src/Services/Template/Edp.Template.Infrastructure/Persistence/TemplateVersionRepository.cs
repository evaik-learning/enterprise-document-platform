using Edp.Template.Application.Contracts;
using Edp.Template.Domain.Entities;
using Edp.Template.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edp.Template.Infrastructure.Persistence;

public sealed class TemplateVersionRepository : ITemplateVersionRepository
{
    private readonly TemplateDbContext _db;

    public TemplateVersionRepository(TemplateDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(TemplateVersion version, CancellationToken cancellationToken = default)
    {
        await _db.TemplateVersions.AddAsync(version, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TemplateVersion>> GetByTemplateIdAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _db.TemplateVersions
            .Where(v => v.TemplateId == templateId && v.OrganizationId == organizationId)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextVersionNumberAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var max = await _db.TemplateVersions.Where(v => v.TemplateId == templateId).MaxAsync(v => (int?)v.VersionNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task<TemplateVersion?> GetByIdAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        return await _db.TemplateVersions.FirstOrDefaultAsync(
            v => v.Id == versionId && v.TemplateId == templateId && v.OrganizationId == organizationId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TemplateVersion>> GetActiveVersionsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _db.TemplateVersions
            .Where(v => v.TemplateId == templateId && v.Status == TemplateVersionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(TemplateVersion version, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
