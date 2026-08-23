using Edp.Document.Application.Interfaces;
using Edp.Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edp.Document.Infrastructure.Persistence;

public sealed class DocumentGenerationJobRepository : IDocumentGenerationJobRepository
{
    private readonly DocumentDbContext _dbContext;

    public DocumentGenerationJobRepository(DocumentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DocumentGenerationJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentGenerationJobs.FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
    }

    public Task<IReadOnlyList<DocumentGenerationJob>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DocumentGenerationJob>>(
            _dbContext.DocumentGenerationJobs
                .Where(x => x.Status == "Queued" || x.Status == "Processing")
                .OrderBy(x => x.StartedAt)
                .ToList());
    }

    public async Task<bool> TryStartProcessingAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var rowsUpdated = await _dbContext.DocumentGenerationJobs
            .Where(x => x.Id == jobId && x.Status == "Queued")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "Processing")
                .SetProperty(x => x.StartedAt, x => x.StartedAt ?? now)
                .SetProperty(x => x.ModifiedAt, now), cancellationToken);

        return rowsUpdated == 1;
    }

    public async Task AddAsync(DocumentGenerationJob job, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentGenerationJobs.AddAsync(job, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DocumentGenerationJob job, CancellationToken cancellationToken = default)
    {
        _dbContext.DocumentGenerationJobs.Update(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
