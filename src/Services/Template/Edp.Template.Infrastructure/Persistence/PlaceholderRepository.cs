using Edp.Template.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Edp.Template.Infrastructure.Persistence;

public sealed class PlaceholderRepository : IPlaceholderRepository
{
    private readonly TemplateDbContext _db;

    public PlaceholderRepository(TemplateDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(global::Edp.Template.Domain.Entities.Placeholder placeholder, CancellationToken cancellationToken = default)
    {
        await _db.Placeholders.AddAsync(placeholder, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<global::Edp.Template.Domain.Entities.Placeholder> placeholders, CancellationToken cancellationToken = default)
    {
        await _db.Placeholders.AddRangeAsync(placeholders, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<global::Edp.Template.Domain.Entities.Placeholder?> GetByIdAsync(Guid placeholderId, CancellationToken cancellationToken = default)
    {
        return await _db.Placeholders.FirstOrDefaultAsync(p => p.Id == placeholderId, cancellationToken);
    }

    public async Task<global::Edp.Template.Domain.Entities.Placeholder?> GetByVersionAndIdAsync(Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default)
    {
        return await _db.Placeholders.FirstOrDefaultAsync(p => p.TemplateVersionId == versionId && p.Id == placeholderId, cancellationToken);
    }

    public async Task<IReadOnlyList<global::Edp.Template.Domain.Entities.Placeholder>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await _db.Placeholders.Where(p => p.TemplateVersionId == versionId).ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(global::Edp.Template.Domain.Entities.Placeholder placeholder, CancellationToken cancellationToken = default)
    {
        _db.Placeholders.Update(placeholder);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(global::Edp.Template.Domain.Entities.Placeholder placeholder, CancellationToken cancellationToken = default)
    {
        _db.Placeholders.Remove(placeholder);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
