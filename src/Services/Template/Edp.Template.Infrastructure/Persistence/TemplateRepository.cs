using Edp.Template.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Edp.Template.Infrastructure.Persistence;

public sealed class TemplateRepository : ITemplateRepository
{
    private readonly TemplateDbContext _db;

    public TemplateRepository(TemplateDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(global::Edp.Template.Domain.Entities.Template templateEntity, CancellationToken cancellationToken = default)
    {
        await _db.Templates.AddAsync(templateEntity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<global::Edp.Template.Domain.Entities.Template?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Templates.FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(Guid organizationId, string code, CancellationToken cancellationToken = default)
    {
        return await _db.Templates.AnyAsync(t => t.OrganizationId == organizationId && t.Code == code, cancellationToken);
    }

    public async Task UpdateAsync(global::Edp.Template.Domain.Entities.Template templateEntity, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)> SearchAsync(
        Guid organizationId,
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Templates.Where(t => t.OrganizationId == organizationId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search) || t.Code.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Edp.Template.Domain.Enums.TemplateStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(t => t.Status == parsedStatus);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
