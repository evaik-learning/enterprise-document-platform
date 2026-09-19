using NotificationEntity = Edp.Notification.Domain.Entities.Notification;
using Edp.Notification.Domain.Entities;

namespace Edp.Notification.Application.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationEntity>> ListAsync(
        Guid organizationId,
        Guid userId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<NotificationEntity?> GetAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForSourceEventAsync(Guid organizationId, Guid userId, Guid sourceEventId, NotificationChannel channel, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
    Task MarkReadAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
}
