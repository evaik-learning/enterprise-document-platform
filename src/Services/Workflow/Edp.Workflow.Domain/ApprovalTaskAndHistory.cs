using Edp.SharedKernel.Domain;
using Edp.SharedKernel.Entities;

namespace Edp.Workflow.Domain;

/// <summary>
/// Approval task - represents an approval action that needs to be taken
/// </summary>
public class ApprovalTask : AuditableEntity<Guid>
{
    /// <summary>Workflow instance this task belongs to</summary>
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>Organization this task belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>State this approval is for</summary>
    public Guid StateId { get; set; }

    /// <summary>Current status of this task</summary>
    public ApprovalStatus Status { get; set; }

    /// <summary>User assigned to approve this task</summary>
    public Guid AssignedToUserId { get; set; }

    /// <summary>Deadline for approval</summary>
    public DateTime? DeadlineAt { get; set; }

    /// <summary>Sequence/order if multiple tasks for same state</summary>
    public int Sequence { get; set; }

    /// <summary>Previous assignee if reassigned</summary>
    public Guid? ReassignedFromUserId { get; set; }

    /// <summary>All approval actions on this task</summary>
    private List<ApprovalAction> _actions = new();
    public IReadOnlyList<ApprovalAction> Actions => _actions.AsReadOnly();

    /// <summary>Optimistic concurrency control</summary>
    public byte[]? RowVersion { get; set; }

    public ApprovalTask() { }

    public ApprovalTask(Guid workflowInstanceId, Guid stateId, Guid assignedToUserId, Guid organizationId, DateTime? deadlineAt = null)
    {
        Id = Guid.NewGuid();
        WorkflowInstanceId = workflowInstanceId;
        StateId = stateId;
        OrganizationId = organizationId;
        AssignedToUserId = assignedToUserId;
        DeadlineAt = deadlineAt;
        Status = ApprovalStatus.Pending;
        Sequence = 1;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Approve this task</summary>
    public void Approve(Guid approvedBy, string? comment = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Cannot approve task in '{Status}' status");

        if (approvedBy != AssignedToUserId && ReassignedFromUserId != approvedBy)
            throw new ApprovalNotPermittedException(Id, approvedBy);

        if (DeadlineAt.HasValue && DateTime.UtcNow > DeadlineAt)
            throw new ApprovalDeadlineExpiredException(Id, DeadlineAt.Value);

        Status = ApprovalStatus.Approved;
        ModifiedBy = approvedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        var action = new ApprovalAction(Id, ApprovalStatus.Approved, approvedBy, comment);
        _actions.Add(action);
    }

    /// <summary>Reject this task</summary>
    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Cannot reject task in '{Status}' status");

        if (rejectedBy != AssignedToUserId && ReassignedFromUserId != rejectedBy)
            throw new ApprovalNotPermittedException(Id, rejectedBy);

        Status = ApprovalStatus.Rejected;
        ModifiedBy = rejectedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        var action = new ApprovalAction(Id, ApprovalStatus.Rejected, rejectedBy, reason);
        _actions.Add(action);
    }

    /// <summary>Reassign this task to another user</summary>
    public void Reassign(Guid toUserId, Guid reassignedBy, string? reason = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Cannot reassign task in '{Status}' status");

        if (reassignedBy != AssignedToUserId)
            throw new UnauthorizedAccessException("Only assigned user can reassign approval tasks");

        ReassignedFromUserId = AssignedToUserId;
        AssignedToUserId = toUserId;
        Status = ApprovalStatus.Reassigned;
        ModifiedBy = reassignedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        var action = new ApprovalAction(Id, ApprovalStatus.Reassigned, reassignedBy, reason ?? "Task reassigned");
        _actions.Add(action);

        // Reset status to pending after reassignment
        Status = ApprovalStatus.Pending;
    }

    /// <summary>Cancel this task</summary>
    public void Cancel()
    {
        if (Status != ApprovalStatus.Pending && Status != ApprovalStatus.Reassigned)
            return; // Already terminated

        Status = ApprovalStatus.Cancelled;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Mark task as expired if deadline passed</summary>
    public void MarkExpiredIfNeeded()
    {
        if (Status == ApprovalStatus.Pending && DeadlineAt.HasValue && DateTime.UtcNow > DeadlineAt.Value)
        {
            Status = ApprovalStatus.Expired;
            ModifiedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new ApprovalExpiredDomainEvent(
                Id, WorkflowInstanceId, StateId, OrganizationId, DateTime.UtcNow));
        }
    }

    /// <summary>Get approval decision (if approved or rejected)</summary>
    public ApprovalAction? GetDecision() =>
        _actions.FirstOrDefault(a => a.ActionType == ApprovalStatus.Approved || a.ActionType == ApprovalStatus.Rejected);
}

/// <summary>
/// Approval action - records an action taken on an approval task
/// </summary>
public class ApprovalAction : AuditableEntity<Guid>
{
    /// <summary>Approval task this action belongs to</summary>
    public Guid ApprovalTaskId { get; set; }

    /// <summary>Organization this action belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Type of action (Approved, Rejected, Reassigned, etc.)</summary>
    public ApprovalStatus ActionType { get; set; }

    /// <summary>User who took this action</summary>
    public Guid ActionByUserId { get; set; }

    /// <summary>Comment or reason for the action</summary>
    public string? Comment { get; set; }

    /// <summary>When this action was taken</summary>
    public DateTime ActionAt { get; set; }

    public ApprovalAction() { }

    public ApprovalAction(Guid approvalTaskId, ApprovalStatus actionType, Guid actionByUserId, string? comment = null)
    {
        Id = Guid.NewGuid();
        ApprovalTaskId = approvalTaskId;
        ActionType = actionType;
        ActionByUserId = actionByUserId;
        Comment = comment;
        ActionAt = DateTime.UtcNow;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = actionByUserId.ToString();
    }
}

/// <summary>
/// Workflow history - records events in workflow execution
/// </summary>
public class WorkflowHistory : AuditableEntity<Guid>
{
    /// <summary>Workflow instance this history entry belongs to</summary>
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>Organization this history belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Type of history event</summary>
    public HistoryEventType EventType { get; set; }

    /// <summary>State involved (if applicable)</summary>
    public Guid? StateId { get; set; }

    /// <summary>Approval task involved (if applicable)</summary>
    public Guid? ApprovalTaskId { get; set; }

    /// <summary>User involved in the event</summary>
    public Guid? UserId { get; set; }

    /// <summary>Event description/details</summary>
    public string Description { get; set; } = null!;

    /// <summary>Additional data (JSON)</summary>
    public string? Data { get; set; }

    /// <summary>When this event occurred</summary>
    public DateTime EventAt { get; set; }

    public WorkflowHistory() { }

    public WorkflowHistory(
        Guid workflowInstanceId,
        Guid organizationId,
        HistoryEventType eventType,
        string description,
        Guid? stateId = null,
        Guid? approvalTaskId = null,
        Guid? userId = null,
        string? data = null)
    {
        Id = Guid.NewGuid();
        WorkflowInstanceId = workflowInstanceId;
        OrganizationId = organizationId;
        EventType = eventType;
        StateId = stateId;
        ApprovalTaskId = approvalTaskId;
        UserId = userId;
        Description = description;
        Data = data;
        EventAt = DateTime.UtcNow;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
