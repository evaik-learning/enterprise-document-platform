using Edp.Template.Application.Contracts;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Template.Infrastructure.Outbox;

public sealed class TemplateOutboxRepository : IOutboxMessageRepository
{
    private readonly EdpDbContext _db;

    public TemplateOutboxRepository(EdpDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _db.TemplateOutboxMessages.AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount = 20, CancellationToken cancellationToken = default)
    {
        return await _db.TemplateOutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _db.TemplateOutboxMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.MarkProcessed();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default)
    {
        var message = await _db.TemplateOutboxMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.MarkFailed(errorMessage);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
