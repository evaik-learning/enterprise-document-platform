using Edp.Template.Application.Contracts;
using Edp.Template.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edp.Template.Infrastructure.Persistence;

public class PlaceholderRepository : IPlaceholderRepository
{
    private readonly TemplateDbContext _db;

    public PlaceholderRepository(TemplateDbContext db)
    {
        _db = db;
    }

    public async Task AddRangeAsync(IEnumerable<global::Edp.Template.Domain.Entities.Placeholder> placeholders, CancellationToken cancellationToken = default)
    {
        await _db.Placeholders.AddRangeAsync(placeholders, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<global::Edp.Template.Domain.Entities.Placeholder>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await _db.Placeholders.Where(p => p.TemplateVersionId == versionId).ToListAsync(cancellationToken);
    }
}
