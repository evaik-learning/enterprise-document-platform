using System.Collections.ObjectModel;

namespace Edp.Workflow.Domain;

public sealed record WorkflowExecutionContext
{
    public Guid WorkflowInstanceId { get; }
    public Guid DocumentId { get; }
    public Guid OrganizationId { get; }
    public Guid ActorUserId { get; }
    public IReadOnlyList<string> ActorRoles { get; }
    public IReadOnlyDictionary<string, object?> Variables { get; }
    public string CorrelationId { get; }

    public WorkflowExecutionContext(
        Guid workflowInstanceId,
        Guid documentId,
        Guid organizationId,
        Guid actorUserId,
        IEnumerable<string>? actorRoles,
        IReadOnlyDictionary<string, object?>? variables,
        string correlationId)
    {
        WorkflowInstanceId = workflowInstanceId;
        DocumentId = documentId;
        OrganizationId = organizationId;
        ActorUserId = actorUserId;
        ActorRoles = new ReadOnlyCollection<string>((actorRoles ?? []).ToList());
        Variables = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(variables ?? new Dictionary<string, object?>(), StringComparer.OrdinalIgnoreCase));
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? throw new ArgumentException("Correlation ID is required", nameof(correlationId))
            : correlationId;
    }
}
