using Edp.Document.Application.Contracts;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Document.Infrastructure.Messaging;

public sealed class DocumentOutboxRepository : IDocumentOutboxMessageRepository
{
    private readonly EdpDbContext _db;

    public DocumentOutboxRepository(EdpDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(DocumentOutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _db.DocumentOutboxMessages.AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentOutboxMessage>> GetPendingAsync(int maxCount = 20, CancellationToken cancellationToken = default)
    {
        return await _db.DocumentOutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _db.DocumentOutboxMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.MarkProcessed();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default)
    {
        var message = await _db.DocumentOutboxMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.MarkFailed(errorMessage);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
