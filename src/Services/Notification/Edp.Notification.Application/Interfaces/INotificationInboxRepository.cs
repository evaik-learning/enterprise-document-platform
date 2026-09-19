using Edp.Notification.Domain.Entities;

namespace Edp.Notification.Application.Interfaces;

public interface INotificationInboxRepository
{
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationInboxMessage message, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken = default);
}
