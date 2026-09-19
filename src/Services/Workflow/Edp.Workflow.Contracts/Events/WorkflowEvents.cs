namespace Edp.Workflow.Contracts.Events;

public sealed record DocumentGeneratedEvent(
    Guid EventId,
    Guid DocumentId,
    Guid OrganizationId,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    Guid? WorkflowId = null);

public sealed record WorkflowCreatedEvent(Guid EventId, Guid WorkflowId, Guid OrganizationId, DateTimeOffset OccurredAt, string CorrelationId);
public sealed record WorkflowPublishedEvent(Guid EventId, Guid WorkflowId, Guid WorkflowVersionId, Guid OrganizationId, DateTimeOffset OccurredAt, string CorrelationId);
public sealed record WorkflowFailedEvent(Guid EventId, Guid WorkflowInstanceId, Guid DocumentId, Guid OrganizationId, string Error, DateTimeOffset OccurredAt, string CorrelationId);
public sealed record ApprovalDelegatedEvent(Guid EventId, Guid ApprovalTaskId, Guid WorkflowInstanceId, Guid FromUserId, Guid ToUserId, Guid OrganizationId, DateTimeOffset OccurredAt, string CorrelationId);

public sealed record WorkflowStartedEvent(
    Guid EventId,
    Guid WorkflowInstanceId,
    Guid WorkflowId,
    int WorkflowVersion,
    Guid DocumentId,
    Guid OrganizationId,
    Guid StartedBy,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record WorkflowCompletedEvent(
    Guid EventId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid OrganizationId,
    DateTimeOffset CompletedAt,
    string CorrelationId);

public sealed record WorkflowCancelledEvent(
    Guid EventId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid CancelledBy,
    string Reason,
    Guid OrganizationId,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record WorkflowStateChangedEvent(
    Guid EventId,
    Guid WorkflowInstanceId,
    Guid FromStateId,
    Guid ToStateId,
    Guid OrganizationId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record ApprovalAssignedEvent(
    Guid EventId,
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid? AssignedUserId,
    string? AssignedRole,
    DateTimeOffset? DueAt,
    Guid OrganizationId,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record ApprovalApprovedEvent(
    Guid EventId,
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid ActorUserId,
    string? Comments,
    Guid OrganizationId,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record ApprovalRejectedEvent(
    Guid EventId,
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid ActorUserId,
    string Comments,
    Guid OrganizationId,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record ApprovalExpiredEvent(
    Guid EventId,
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid OrganizationId,
    DateTimeOffset OccurredAt,
    string CorrelationId);
