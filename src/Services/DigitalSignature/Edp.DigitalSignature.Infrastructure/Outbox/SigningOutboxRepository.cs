namespace Edp.DigitalSignature.Infrastructure.Outbox;

using Microsoft.EntityFrameworkCore;
using Edp.DigitalSignature.Infrastructure.Persistence;

public sealed class SigningOutboxRepository
{
    private readonly SigningDbContext _dbContext;

    public SigningOutboxRepository(SigningDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken cancellationToken)
        => await _dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .Where(message => message.RetryCount < 10)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.MarkProcessed();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.MarkFailed(error);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}