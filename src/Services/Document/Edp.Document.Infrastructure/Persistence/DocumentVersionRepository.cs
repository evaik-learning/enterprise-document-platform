using Edp.Document.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using DocumentVersionEntity = global::Edp.Document.Domain.Entities.DocumentVersion;

namespace Edp.Document.Infrastructure.Persistence;

public sealed class DocumentVersionRepository : IDocumentVersionRepository
{
    private readonly DocumentDbContext _dbContext;

    public DocumentVersionRepository(DocumentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DocumentVersionEntity?> GetLatestAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentVersions
            .Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DocumentVersionEntity>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DocumentVersionEntity>>(
            _dbContext.DocumentVersions
                .Where(x => x.DocumentId == documentId)
                .OrderByDescending(x => x.VersionNumber)
                .ToList());
    }

    public async Task AddAsync(DocumentVersionEntity version, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentVersions.AddAsync(version, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
