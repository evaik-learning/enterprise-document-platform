using Edp.Template.Application.Contracts;
using Edp.Template.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edp.Template.Infrastructure.Persistence;

public class TemplateRepository : ITemplateRepository
{
    private readonly TemplateDbContext _db;

    public TemplateRepository(TemplateDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(global::Edp.Template.Domain.Entities.Template template, CancellationToken cancellationToken = default)
    {
        await _db.Templates.AddAsync(template, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<global::Edp.Template.Domain.Entities.Template?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Templates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(global::Edp.Template.Domain.Entities.Template template, CancellationToken cancellationToken = default)
    {
        try
        {
            _db.Templates.Update(template);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            throw;
        }
    }

    public async Task<(IEnumerable<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)> SearchAsync(
        string? name = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Templates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(t => t.Name.Contains(name));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
