using NotificationEntity = Edp.Notification.Domain.Entities.Notification;

namespace Edp.Notification.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationEntity>> ListAsync(Guid organizationId, Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountUnreadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationEntity?> GetAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task CreateInAppAsync(Guid organizationId, Guid userId, string type, string subject, string body, Guid sourceEventId, string correlationId, CancellationToken cancellationToken = default);
}
