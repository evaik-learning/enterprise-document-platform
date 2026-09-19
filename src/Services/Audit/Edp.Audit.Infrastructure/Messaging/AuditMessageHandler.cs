using System.Text.Json;
using Edp.Audit.Application.Commands;
using Edp.Audit.Application.Interfaces;
using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Audit.Infrastructure.Messaging;

public sealed class AuditMessageHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AuditMessageHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(EventEnvelope envelope, string messageId, CancellationToken cancellationToken = default)
    {
        if (!envelope.OrganizationId.HasValue || envelope.OrganizationId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Audit events must contain a valid organization ID.");
        }

        using var scope = _scopeFactory.CreateScope();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        if (await auditLogService.ExistsForEventAsync(envelope.OrganizationId.Value, envelope.EventId, cancellationToken))
            return;
        var data = envelope.Data as JsonElement?;
        var entityId = ReadGuid(data, "DocumentId")
            ?? ReadGuid(data, "TemplateId")
            ?? ReadGuid(data, "WorkflowInstanceId")
            ?? envelope.EventId;

        var eventType = string.IsNullOrWhiteSpace(envelope.EventType) ? "IntegrationEvent" : envelope.EventType;
        var entityType = eventType.EndsWith("DomainEvent", StringComparison.Ordinal)
            ? eventType[..^"DomainEvent".Length]
            : eventType;

        await auditLogService.RecordAsync(
            new RecordAuditEventCommand(
                envelope.OrganizationId.Value,
                envelope.UserId,
                eventType,
                entityType,
                entityId,
                envelope.CorrelationId?.ToString() ?? envelope.EventId.ToString("N"),
                "service-bus",
                new Dictionary<string, object?>
                {
                    ["messageId"] = messageId,
                    ["eventId"] = envelope.EventId,
                    ["occurredAt"] = envelope.OccurredAt,
                    ["eventData"] = envelope.Data is null ? null : JsonSerializer.Serialize(envelope.Data)
                },
                envelope.EventId),
            cancellationToken);
    }

    private static Guid? ReadGuid(JsonElement? data, string propertyName)
    {
        if (data is not JsonElement element || element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property) ||
            !property.TryGetGuid(out var value) || value == Guid.Empty)
        {
            return null;
        }

        return value;
    }
}
