using System.Text.Json;
using Edp.Notification.Application.Interfaces;
using Edp.Notification.Domain.Entities;
using NotificationEntity = Edp.Notification.Domain.Entities.Notification;
using Edp.Shared.Contracts;
using Edp.Shared.Infrastructure.Persistence;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Edp.Notification.Infrastructure.Messaging;

public sealed class NotificationMessageHandler : IMessageHandler
{
    private static readonly HashSet<string> SupportedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApprovalAssignedEvent",
        "ApprovalApprovedEvent",
        "ApprovalRejectedEvent",
        "WorkflowCompletedEvent",
        "WorkflowCancelledEvent",
        "WorkflowFailedEvent",
        "ApprovalDelegatedEvent",
        "ApprovalExpiredEvent",
        "WorkflowStartedEvent",
        "WorkflowStateChangedEvent",
        "DocumentGeneratedEvent",
        "DocumentCreatedDomainEvent",
        "TemplateActivatedDomainEvent",
        "SignatureRequestedEvent",
        "SignatureCompletedEvent",
        "SignatureDeclinedEvent",
        "SignatureExpiredEvent"
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationMessageHandler> _logger;

    public NotificationMessageHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationMessageHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(
        EventEnvelope envelope,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (envelope.EventId == Guid.Empty)
            throw new InvalidOperationException("Notification events must contain a valid event ID.");
        if (!envelope.OrganizationId.HasValue || envelope.OrganizationId.Value == Guid.Empty)
            throw new InvalidOperationException("Notification events must contain a valid organization ID.");

        if (!SupportedEventTypes.Contains(envelope.EventType))
        {
            _logger.LogDebug("Ignoring unsupported notification event {EventType}.", envelope.EventType);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<INotificationInboxRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INotificationUnitOfWork>();

        if (await inbox.ExistsAsync(messageId, cancellationToken))
            return;

        var payload = envelope.Data is null
            ? "null"
            : JsonSerializer.Serialize(envelope.Data);
        await inbox.AddAsync(
            new NotificationInboxMessage(
                envelope.OrganizationId.Value,
                messageId,
                envelope.EventType,
                payload),
            cancellationToken);
        var recipientId = ReadGuid(envelope.Data, "AssignedUserId")
            ?? ReadGuid(envelope.Data, "AssignedToUserId")
            ?? ReadGuid(envelope.Data, "ApproverUserId")
            ?? ReadGuid(envelope.Data, "SignerUserId")
            ?? ReadGuid(envelope.Data, "ToUserId")
            ?? ReadGuid(envelope.Data, "ActorUserId")
            ?? envelope.UserId;
        if (recipientId.HasValue)
        {
            if (await notificationRepository.ExistsForSourceEventAsync(
                    envelope.OrganizationId.Value,
                    recipientId.Value,
                    envelope.EventId,
                    NotificationChannel.InApp,
                    cancellationToken))
            {
                return;
            }

            var notificationType = envelope.EventType.Replace("Event", string.Empty, StringComparison.OrdinalIgnoreCase);
            await notificationRepository.AddAsync(
                NotificationEntity.Create(
                    envelope.OrganizationId.Value,
                    recipientId.Value,
                    notificationType,
                    notificationType,
                    $"You have a new {notificationType} notification.",
                    envelope.EventId,
                    envelope.CorrelationId?.ToString() ?? envelope.EventId.ToString("N")),
                cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await inbox.MarkProcessedAsync(messageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Guid? ReadGuid(object? data, string propertyName)
    {
        if (data is not JsonElement element || element.ValueKind != JsonValueKind.Object) return null;
        if (!element.TryGetProperty(propertyName, out var property) || !property.TryGetGuid(out var value) || value == Guid.Empty) return null;
        return value;
    }
}
