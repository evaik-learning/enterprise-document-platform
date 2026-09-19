using Edp.Notification.Application.Interfaces;
using NotificationEntity = Edp.Notification.Domain.Entities.Notification;
using Edp.Notification.Domain.Entities;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Notification.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly EdpDbContext _dbContext;

    public NotificationRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<NotificationEntity>> ListAsync(Guid organizationId, Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.UserId == userId);
        if (unreadOnly) query = query.Where(x => x.ReadAtUtc == null);
        return await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public Task<NotificationEntity?> GetAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId && x.UserId == userId, cancellationToken);

    public Task<bool> ExistsForSourceEventAsync(Guid organizationId, Guid userId, Guid sourceEventId, NotificationChannel channel, CancellationToken cancellationToken = default) =>
        _dbContext.Notifications.AnyAsync(
            x => x.OrganizationId == organizationId
                && x.UserId == userId
                && x.SourceEventId == sourceEventId
                && x.Channel == channel,
            cancellationToken);

    public async Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task MarkReadAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        notification.MarkRead();
    }

    public async Task MarkAllReadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.Notifications
            .Where(x => x.OrganizationId == organizationId && x.UserId == userId && x.ReadAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var notification in notifications) notification.MarkRead();
    }
}
