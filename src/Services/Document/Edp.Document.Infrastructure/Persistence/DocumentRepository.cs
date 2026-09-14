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
