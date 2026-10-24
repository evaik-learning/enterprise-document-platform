using Edp.Notification.Application.Interfaces;
using Edp.Notification.Domain.Entities;
using NotificationEntity = Edp.Notification.Domain.Entities.Notification;

namespace Edp.Notification.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly INotificationUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository repository, INotificationPreferenceRepository preferences, INotificationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _preferences = preferences;
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var preference = await _preferences.GetAsync(organizationId, userId, cancellationToken)
            ?? NotificationPreference.CreateDefault(organizationId, userId);
        return ToDto(preference);
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid organizationId, Guid userId, NotificationPreferencesDto preferences, CancellationToken cancellationToken = default)
    {
        var preference = await _preferences.GetAsync(organizationId, userId, cancellationToken);
        if (preference is null)
        {
            preference = NotificationPreference.CreateDefault(organizationId, userId);
            await _preferences.AddAsync(preference, cancellationToken);
        }

        preference.Update(preferences.EmailEnabled, preferences.InAppEnabled, preferences.WorkflowEnabled, preferences.SignatureEnabled, preferences.AuditEnabled);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(preference);
    }

    private static NotificationPreferencesDto ToDto(NotificationPreference preference) =>
        new(preference.EmailEnabled, preference.InAppEnabled, preference.WorkflowEnabled, preference.SignatureEnabled, preference.AuditEnabled);

    public Task<IReadOnlyList<NotificationEntity>> ListAsync(Guid organizationId, Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default) =>
        _repository.ListAsync(organizationId, userId, unreadOnly, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), cancellationToken);

    public Task<int> CountUnreadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default) =>
        _repository.CountUnreadAsync(organizationId, userId, cancellationToken);

    public Task<NotificationEntity?> GetAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetAsync(organizationId, userId, id, cancellationToken);

    public async Task MarkReadAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetAsync(organizationId, userId, id, cancellationToken);
        if (notification is not null)
        {
            await _repository.MarkReadAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllReadAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _repository.MarkAllReadAsync(organizationId, userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateInAppAsync(Guid organizationId, Guid userId, string type, string subject, string body, Guid sourceEventId, string correlationId, CancellationToken cancellationToken = default)
    {
        var notification = NotificationEntity.Create(organizationId, userId, type, subject, body, sourceEventId, correlationId);
        await _repository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
