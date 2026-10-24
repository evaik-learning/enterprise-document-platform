using Edp.Notification.Domain.Entities;

namespace Edp.Notification.Application.Interfaces;

public sealed record NotificationPreferencesDto(
    bool EmailEnabled,
    bool InAppEnabled,
    bool WorkflowEnabled,
    bool SignatureEnabled,
    bool AuditEnabled);

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
}
