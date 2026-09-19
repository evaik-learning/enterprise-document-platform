using Edp.Workflow.Application.Contracts;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Workflow.Infrastructure.Outbox;

public sealed class WorkflowOutboxRepository : IOutboxMessageRepository
{
    private readonly EdpDbContext _db;

    public WorkflowOutboxRepository(EdpDbContext db) => _db = db;

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _db.WorkflowOutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, int maxRetryAttempts, CancellationToken cancellationToken = default) =>
        await _db.WorkflowOutboxMessages
            .Where(message => message.ProcessedOnUtc == null && message.Status == OutboxStatus.Pending && message.RetryCount < maxRetryAttempts)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _db.WorkflowOutboxMessages.FirstOrDefaultAsync(message => message.Id == id, cancellationToken);
        if (message is null)
            return;

        message.MarkProcessed();
    }

    public async Task MarkFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default)
    {
        var message = await _db.WorkflowOutboxMessages.FirstOrDefaultAsync(message => message.Id == id, cancellationToken);
        if (message is null)
            return;

        message.MarkFailed(errorMessage);
    }

    public async Task MarkDeadLetterAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default)
    {
        var message = await _db.WorkflowOutboxMessages.FirstOrDefaultAsync(message => message.Id == id, cancellationToken);
        if (message is null)
            return;

        message.MarkDeadLetter(errorMessage);
    }
}
