using Edp.SharedKernel.Domain;
using Edp.SharedKernel.Entities;

namespace Edp.Workflow.Domain;

/// <summary>
/// Workflow instance - represents a running instance of a workflow
/// </summary>
public class WorkflowInstance : AuditableEntity<Guid>
{
    /// <summary>Organization this instance belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Document this workflow instance is processing</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Workflow definition ID</summary>
    public Guid WorkflowId { get; set; }

    public Guid WorkflowVersionId { get; set; }

    /// <summary>Workflow version being executed</summary>
    public int WorkflowVersion { get; set; }

    /// <summary>Current execution status</summary>
    public InstanceStatus Status { get; set; }

    /// <summary>Currently active state</summary>
    public Guid? CurrentStateId { get; set; }

    /// <summary>When the instance started execution</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When the instance completed/terminated</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>User who initiated this instance</summary>
    public Guid InitiatedBy { get; set; }

    /// <summary>For rejected instances, the reason</summary>
    public string? RejectionReason { get; set; }

    /// <summary>All workflow variable values for this instance</summary>
    private List<WorkflowVariable> _variables = new();
    public IReadOnlyList<WorkflowVariable> Variables => _variables.AsReadOnly();

    /// <summary>All state instances (history of states entered)</summary>
    private List<WorkflowStateInstance> _stateInstances = new();
    public IReadOnlyList<WorkflowStateInstance> StateInstances => _stateInstances.AsReadOnly();

    /// <summary>All approval tasks for this instance</summary>
    private List<ApprovalTask> _approvalTasks = new();
    public IReadOnlyList<ApprovalTask> ApprovalTasks => _approvalTasks.AsReadOnly();

    /// <summary>Optimistic concurrency control</summary>
    public byte[]? RowVersion { get; set; }

    public WorkflowInstance() { }

    public WorkflowInstance(Guid organizationId, Guid documentId, Guid workflowId, int workflowVersion, Guid initiatedBy, Guid workflowVersionId = default)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        DocumentId = documentId;
        WorkflowId = workflowId;
        WorkflowVersion = workflowVersion;
        WorkflowVersionId = workflowVersionId;
        Status = InstanceStatus.Pending;
        InitiatedBy = initiatedBy;
        CreatedBy = initiatedBy.ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Start this workflow instance</summary>
    public void Start(Guid startStateId, Guid startedBy)
    {
        if (Status != InstanceStatus.Pending)
            throw new InvalidWorkflowStateException(Id, Status, "start");

        Status = InstanceStatus.InProgress;
        CurrentStateId = startStateId;
        StartedAt = DateTime.UtcNow;
        ModifiedBy = startedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        // Record state entry
        var stateInstance = new WorkflowStateInstance(Id, startStateId, OrganizationId);
        _stateInstances.Add(stateInstance);

        AddDomainEvent(new WorkflowStartedDomainEvent(
            Id, WorkflowId, DocumentId, InitiatedBy, OrganizationId
        ));
    }

    /// <summary>Transition to a new state</summary>
    public void TransitionToState(Guid toStateId, Guid transitionedBy)
    {
        if (Status != InstanceStatus.InProgress && Status != InstanceStatus.AwaitingApproval)
            throw new InvalidWorkflowStateException(Id, Status, "transition");

        var previousStateId = CurrentStateId;
        CurrentStateId = toStateId;
        ModifiedBy = transitionedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        // Record state entry
        var stateInstance = new WorkflowStateInstance(Id, toStateId, OrganizationId);
        _stateInstances.Add(stateInstance);

        if (previousStateId.HasValue)
        {
            AddDomainEvent(new WorkflowTransitionedDomainEvent(
                Id, previousStateId.Value, toStateId, "", StateType.Task, OrganizationId
            ));
        }
    }

    /// <summary>Set variable value</summary>
    public void SetVariable(string name, string value)
    {
        var existing = _variables.FirstOrDefault(v => v.Name == name);
        if (existing != null)
        {
            _variables.Remove(existing);
        }

        _variables.Add(new WorkflowVariable(name, value));
    }

    /// <summary>Get variable value</summary>
    public string? GetVariable(string name) => _variables.FirstOrDefault(v => v.Name == name)?.Value;

    /// <summary>Get variable value or throw if not found</summary>
    public string GetRequiredVariable(string name)
    {
        var value = GetVariable(name);
        if (value == null)
            throw new MissingRequiredVariableException(Id, name);
        return value;
    }

    /// <summary>Create an approval task</summary>
    public ApprovalTask CreateApprovalTask(Guid stateId, Guid assignedToUserId, DateTime? deadlineAt = null)
    {
        if (Status == InstanceStatus.Completed || Status == InstanceStatus.Rejected || Status == InstanceStatus.Cancelled)
            throw new InvalidWorkflowStateException(Id, Status, "create approval task");

        var task = new ApprovalTask(Id, stateId, assignedToUserId, OrganizationId, deadlineAt);
        _approvalTasks.Add(task);

        Status = InstanceStatus.AwaitingApproval;
        ModifiedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ApprovalAssignedDomainEvent(
            task.Id, Id, stateId, "", assignedToUserId, deadlineAt, OrganizationId
        ));

        return task;
    }

    public void RecordApprovalReassigned(Guid taskId, Guid fromUserId, Guid toUserId, string reason)
    {
        AddDomainEvent(new ApprovalReassignedDomainEvent(
            taskId, Id, fromUserId, toUserId, reason, OrganizationId));
    }

    public void RecordApprovalRejected(Guid taskId, Guid stateId, Guid rejectedBy, string reason)
    {
        AddDomainEvent(new ApprovalRejectedDomainEvent(
            taskId, Id, stateId, rejectedBy, reason, OrganizationId));
    }

    /// <summary>Complete this workflow instance</summary>
    public void Complete(Guid completedBy)
    {
        if (Status == InstanceStatus.Completed || Status == InstanceStatus.Rejected || Status == InstanceStatus.Cancelled)
            throw new InvalidWorkflowStateException(Id, Status, "complete");

        Status = InstanceStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ModifiedBy = completedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new WorkflowCompletedDomainEvent(
            Id, DocumentId, completedBy, DateTime.UtcNow, OrganizationId
        ));
    }

    /// <summary>Reject this workflow instance</summary>
    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status == InstanceStatus.Completed || Status == InstanceStatus.Rejected || Status == InstanceStatus.Cancelled)
            throw new InvalidWorkflowStateException(Id, Status, "reject");

        Status = InstanceStatus.Rejected;
        RejectionReason = reason;
        CompletedAt = DateTime.UtcNow;
        ModifiedBy = rejectedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        // Cancel all pending approval tasks
        foreach (var task in _approvalTasks.Where(t => t.Status == ApprovalStatus.Pending))
        {
            task.Cancel();
        }

        AddDomainEvent(new WorkflowRejectedDomainEvent(
            Id, DocumentId, rejectedBy, reason, OrganizationId
        ));
    }

    /// <summary>Cancel this workflow instance</summary>
    public void Cancel(Guid cancelledBy, string reason)
    {
        if (Status == InstanceStatus.Completed || Status == InstanceStatus.Rejected || Status == InstanceStatus.Cancelled)
            throw new InvalidWorkflowStateException(Id, Status, "cancel");

        Status = InstanceStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        ModifiedBy = cancelledBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        // Cancel all pending approval tasks
        foreach (var task in _approvalTasks.Where(t => t.Status == ApprovalStatus.Pending))
        {
            task.Cancel();
        }

        AddDomainEvent(new WorkflowCancelledDomainEvent(
            Id, DocumentId, cancelledBy, reason, OrganizationId
        ));
    }

    /// <summary>Pause this workflow instance</summary>
    public void Pause(Guid pausedBy, string reason)
    {
        if (Status != InstanceStatus.InProgress && Status != InstanceStatus.AwaitingApproval)
            throw new InvalidWorkflowStateException(Id, Status, "pause");

        Status = InstanceStatus.Paused;
        ModifiedBy = pausedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new WorkflowPausedDomainEvent(
            Id, pausedBy, reason, OrganizationId
        ));
    }

    /// <summary>Resume this workflow instance</summary>
    public void Resume(Guid resumedBy)
    {
        if (Status != InstanceStatus.Paused)
            throw new InvalidWorkflowStateException(Id, Status, "resume");

        Status = InstanceStatus.InProgress;
        ModifiedBy = resumedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new WorkflowResumedDomainEvent(
            Id, resumedBy, OrganizationId
        ));
    }

    /// <summary>Get approval task by ID</summary>
    public ApprovalTask? GetApprovalTask(Guid taskId) => _approvalTasks.FirstOrDefault(t => t.Id == taskId);

    /// <summary>Get approval task or throw if not found</summary>
    public ApprovalTask GetRequiredApprovalTask(Guid taskId) =>
        GetApprovalTask(taskId) ?? throw new ApprovalTaskNotFoundException(taskId);

    /// <summary>Get all pending approval tasks</summary>
    public IReadOnlyList<ApprovalTask> GetPendingApprovalTasks() =>
        _approvalTasks.Where(t => t.Status == ApprovalStatus.Pending).ToList().AsReadOnly();

}

/// <summary>
/// Workflow state instance - records when a workflow entered a state
/// </summary>
public class WorkflowStateInstance : AuditableEntity<Guid>
{
    /// <summary>Workflow instance this state belongs to</summary>
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>Organization this state belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>The state definition ID</summary>
    public Guid StateId { get; set; }

    /// <summary>When this state was entered</summary>
    public DateTime EnteredAt { get; set; }

    /// <summary>When this state was exited (null if still current)</summary>
    public DateTime? ExitedAt { get; set; }

    public WorkflowStateInstance() { }

    public WorkflowStateInstance(Guid workflowInstanceId, Guid stateId, Guid organizationId)
    {
        Id = Guid.NewGuid();
        WorkflowInstanceId = workflowInstanceId;
        StateId = stateId;
        OrganizationId = organizationId;
        EnteredAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Mark this state as exited</summary>
    public void Exit()
    {
        if (!ExitedAt.HasValue)
            ExitedAt = DateTime.UtcNow;
    }
}
