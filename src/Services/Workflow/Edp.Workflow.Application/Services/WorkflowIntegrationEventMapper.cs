using Edp.Workflow.Contracts.Events;
using Edp.Workflow.Domain;
using Edp.SharedKernel.Domain;

namespace Edp.Workflow.Application.Services;

public static class WorkflowIntegrationEventMapper
{
    public static (string EventType, object Payload)? Map(
        DomainEvent domainEvent,
        WorkflowInstance instance,
        int workflowVersion,
        string correlationId)
    {
        var eventId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;

        return domainEvent switch
        {
            WorkflowStartedDomainEvent started => (nameof(WorkflowStartedEvent), new WorkflowStartedEvent(
                eventId, started.WorkflowInstanceId, started.WorkflowId, workflowVersion, started.DocumentId,
                started.OrganizationId, started.InitiatedBy, occurredAt, correlationId)),
            WorkflowTransitionedDomainEvent transitioned => (nameof(WorkflowStateChangedEvent), new WorkflowStateChangedEvent(
                eventId, transitioned.WorkflowInstanceId, transitioned.FromStateId, transitioned.ToStateId,
                transitioned.OrganizationId, transitioned.ActorUserId, occurredAt, correlationId)),
            ApprovalAssignedDomainEvent assigned => (nameof(ApprovalAssignedEvent), new ApprovalAssignedEvent(
                eventId, assigned.ApprovalTaskId, assigned.WorkflowInstanceId, instance.DocumentId,
                assigned.AssignedToUserId, null, assigned.DeadlineAt, assigned.OrganizationId, occurredAt, correlationId)),
            ApprovalApprovedDomainEvent approved => (nameof(ApprovalApprovedEvent), new ApprovalApprovedEvent(
                eventId, approved.ApprovalTaskId, approved.WorkflowInstanceId, instance.DocumentId,
                approved.ApprovedByUserId, approved.Comment, approved.OrganizationId, occurredAt, correlationId)),
            ApprovalRejectedDomainEvent rejected => (nameof(ApprovalRejectedEvent), new ApprovalRejectedEvent(
                eventId, rejected.ApprovalTaskId, rejected.WorkflowInstanceId, instance.DocumentId,
                rejected.RejectedByUserId, rejected.RejectReason, rejected.OrganizationId, occurredAt, correlationId)),
            ApprovalExpiredDomainEvent expired => (nameof(ApprovalExpiredEvent), new ApprovalExpiredEvent(
                eventId, expired.ApprovalTaskId, expired.WorkflowInstanceId, instance.DocumentId,
                expired.OrganizationId, occurredAt, correlationId)),
            WorkflowCompletedDomainEvent completed => (nameof(WorkflowCompletedEvent), new WorkflowCompletedEvent(
                eventId, completed.WorkflowInstanceId, completed.DocumentId, completed.OrganizationId,
                completed.CompletedAt, correlationId)),
            WorkflowCancelledDomainEvent cancelled => (nameof(WorkflowCancelledEvent), new WorkflowCancelledEvent(
                eventId, cancelled.WorkflowInstanceId, cancelled.DocumentId, cancelled.CancelledBy,
                cancelled.CancellationReason, cancelled.OrganizationId, occurredAt, correlationId)),
            _ => null
        };
    }
}