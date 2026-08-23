using Edp.Document.Application.Interfaces;
using Edp.Document.Domain.Entities;

namespace Edp.Document.Infrastructure.Persistence;

public sealed class DocumentFileRepository : IDocumentFileRepository
{
    private readonly DocumentDbContext _dbContext;

    public DocumentFileRepository(DocumentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DocumentFile file, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentFiles.AddAsync(file, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
