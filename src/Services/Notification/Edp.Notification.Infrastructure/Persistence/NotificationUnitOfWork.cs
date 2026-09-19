using Edp.Notification.Application.Interfaces;
using Edp.Shared.Infrastructure.Persistence;

namespace Edp.Notification.Infrastructure.Persistence;

public sealed class NotificationUnitOfWork(IUnitOfWork inner) : INotificationUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        inner.SaveChangesAsync(cancellationToken);
}