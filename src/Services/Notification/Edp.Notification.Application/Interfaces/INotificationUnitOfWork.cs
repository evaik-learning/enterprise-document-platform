namespace Edp.Notification.Application.Interfaces;

public interface INotificationUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}