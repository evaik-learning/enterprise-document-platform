using Edp.Document.Application.Interfaces;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;
using DocumentEntity = global::Edp.Document.Domain.Entities.Document;

namespace Edp.Document.Infrastructure.Persistence;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly EdpDbContext _dbContext;

    public DocumentRepository(EdpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DocumentEntity?> GetByIdAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Documents.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == documentId, cancellationToken);
    }

    public Task<IReadOnlyList<DocumentEntity>> ListAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DocumentEntity>>(
            _dbContext.Documents
                .Where(x => x.OrganizationId == organizationId)
                .ToList());
    }

    public async Task<(IReadOnlyList<DocumentEntity> Items, int TotalCount)> ListPageAsync(Guid organizationId, Edp.Document.Domain.Enums.DocumentStatus? status, Guid? templateId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Documents.Where(x => x.OrganizationId == organizationId);
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (templateId.HasValue)
        {
            query = query.Where(x => x.TemplateId == templateId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(DocumentEntity document, CancellationToken cancellationToken = default)
    {
        await _dbContext.Documents.AddAsync(document, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DocumentEntity document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Update(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
