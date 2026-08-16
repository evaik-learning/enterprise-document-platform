namespace Edp.Audit.Application.Commands;

public sealed record RecordAuditEventCommand(
    Guid OrganizationId,
    Guid? UserId,
    string Action,
    string EntityType,
    Guid EntityId,
    string CorrelationId,
    string IpAddress,
    Dictionary<string, object?>? Metadata = null);
