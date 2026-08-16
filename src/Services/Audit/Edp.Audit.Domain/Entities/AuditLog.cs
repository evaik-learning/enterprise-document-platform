using Edp.SharedKernel.Entities;

namespace Edp.Audit.Domain.Entities;

public sealed class AuditLog : AuditableEntity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public Dictionary<string, object?> Metadata { get; private set; } = new();

    public static AuditLog Create(
        Guid id,
        Guid organizationId,
        Guid? userId,
        string action,
        string entityType,
        Guid entityId,
        string correlationId,
        string ipAddress,
        Dictionary<string, object?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new AuditLog
        {
            Id = id,
            OrganizationId = organizationId,
            UserId = userId,
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = correlationId.Trim(),
            IpAddress = ipAddress.Trim(),
            Metadata = metadata ?? new Dictionary<string, object?>()
        };
    }
}
