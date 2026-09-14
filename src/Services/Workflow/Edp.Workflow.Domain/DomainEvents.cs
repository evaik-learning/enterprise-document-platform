using Edp.SharedKernel.Domain;

namespace Edp.Workflow.Domain;

/// <summary>
/// Workflow started event - fired when a workflow instance begins execution
/// </summary>
public sealed record WorkflowStartedDomainEvent(
    Guid WorkflowInstanceId,
    Guid WorkflowId,
    Guid DocumentId,
    Guid InitiatedBy,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow state entered event - fired when workflow enters a state
/// </summary>
public sealed record WorkflowStateEnteredDomainEvent(
    Guid WorkflowInstanceId,
    Guid StateId,
    string StateName,
    StateType StateType,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow state exited event - fired when workflow leaves a state
/// </summary>
public sealed record WorkflowStateExitedDomainEvent(
    Guid WorkflowInstanceId,
    Guid StateId,
    string StateName,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Approval assigned event - fired when an approval task is assigned
/// </summary>
public sealed record ApprovalAssignedDomainEvent(
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid StateId,
    string StateName,
    Guid AssignedToUserId,
    DateTime? DeadlineAt,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Approval approved event - fired when approver approves
/// </summary>
public sealed record ApprovalApprovedDomainEvent(
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid StateId,
    Guid ApprovedByUserId,
    string? Comment,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Approval rejected event - fired when approver rejects
/// </summary>
public sealed record ApprovalRejectedDomainEvent(
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid StateId,
    Guid RejectedByUserId,
    string RejectReason,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

public sealed record ApprovalExpiredDomainEvent(
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid StateId,
    Guid OrganizationId,
    DateTime ExpiredAt,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow transitioned event - fired when workflow moves to next state
/// </summary>
public sealed record WorkflowTransitionedDomainEvent(
    Guid WorkflowInstanceId,
    Guid FromStateId,
    Guid ToStateId,
    string ToStateName,
    StateType ToStateType,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow completed event - fired when workflow reaches end state
/// </summary>
public sealed record WorkflowCompletedDomainEvent(
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid CompletedBy,
    DateTime CompletedAt,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow rejected event - fired when workflow is rejected
/// </summary>
public sealed record WorkflowRejectedDomainEvent(
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid RejectedBy,
    string RejectionReason,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow cancelled event - fired when workflow is cancelled
/// </summary>
public sealed record WorkflowCancelledDomainEvent(
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid CancelledBy,
    string CancellationReason,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Approval reassigned event - fired when approval task is reassigned
/// </summary>
public sealed record ApprovalReassignedDomainEvent(
    Guid ApprovalTaskId,
    Guid WorkflowInstanceId,
    Guid FromUserId,
    Guid ToUserId,
    string Reason,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow paused event - fired when workflow execution is paused
/// </summary>
public sealed record WorkflowPausedDomainEvent(
    Guid WorkflowInstanceId,
    Guid PausedBy,
    string Reason,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;

/// <summary>
/// Workflow resumed event - fired when paused workflow is resumed
/// </summary>
public sealed record WorkflowResumedDomainEvent(
    Guid WorkflowInstanceId,
    Guid ResumedBy,
    Guid OrganizationId,
    string? CorrelationId = null
) : DomainEvent;
