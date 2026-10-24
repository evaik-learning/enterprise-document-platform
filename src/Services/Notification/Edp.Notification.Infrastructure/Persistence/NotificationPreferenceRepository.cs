using Edp.Notification.Application.Interfaces;
using Edp.Notification.Domain.Entities;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Notification.Infrastructure.Persistence;

public sealed class NotificationPreferenceRepository(EdpDbContext dbContext) : INotificationPreferenceRepository
{
    public Task<NotificationPreference?> GetAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.NotificationPreferences.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == userId, cancellationToken);

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default) =>
        await dbContext.NotificationPreferences.AddAsync(preference, cancellationToken);
}
