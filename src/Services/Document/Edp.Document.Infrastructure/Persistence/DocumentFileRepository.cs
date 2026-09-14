using Edp.Document.Application.Interfaces;
using Edp.Document.Domain.Entities;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Document.Infrastructure.Persistence;

public sealed class DocumentFileRepository : IDocumentFileRepository
{
    private readonly EdpDbContext _dbContext;

    public DocumentFileRepository(EdpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DocumentFile file, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentFiles.AddAsync(file, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<DocumentFile?> GetByVersionIdAsync(Guid documentVersionId, string? fileType = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DocumentFiles
            .Where(x => x.DocumentVersionId == documentVersionId);

        if (!string.IsNullOrWhiteSpace(fileType))
        {
            query = query.Where(x => x.FileType == fileType);
        }

        return query
            .OrderBy(x => x.FileType == "DOCX" ? 0 : 1)
            .ThenBy(x => x.FileName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
