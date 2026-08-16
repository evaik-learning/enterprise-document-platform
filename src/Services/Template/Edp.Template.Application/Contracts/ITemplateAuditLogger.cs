namespace Edp.Template.Application.Contracts;

public interface ITemplateAuditLogger
{
    Task RecordAsync(
        Guid organizationId,
        Guid? userId,
        string action,
        string entityType,
        Guid entityId,
        Dictionary<string, object?>? metadata = null,
        CancellationToken cancellationToken = default);
}
