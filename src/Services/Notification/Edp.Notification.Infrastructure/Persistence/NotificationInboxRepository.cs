using Edp.Notification.Application.Interfaces;
using Edp.Notification.Domain.Entities;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Notification.Infrastructure.Persistence;

public sealed class NotificationInboxRepository : INotificationInboxRepository
{
    private readonly EdpDbContext _dbContext;

    public NotificationInboxRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default) =>
        _dbContext.NotificationInboxMessages.AnyAsync(x => x.MessageId == messageId, cancellationToken);

    public async Task AddAsync(NotificationInboxMessage message, CancellationToken cancellationToken = default) =>
        await _dbContext.NotificationInboxMessages.AddAsync(message, cancellationToken);

    public async Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.NotificationInboxMessages
            .FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
        message?.MarkProcessed();
    }
}
