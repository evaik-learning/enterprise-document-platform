namespace Edp.Workflow.Domain;

/// <summary>Base exception for workflow domain</summary>
public abstract class WorkflowDomainException : Exception
{
    protected WorkflowDomainException(string message) : base(message) { }
    protected WorkflowDomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Workflow not found</summary>
public sealed class WorkflowNotFoundException : WorkflowDomainException
{
    public Guid WorkflowId { get; }
    public WorkflowNotFoundException(Guid workflowId) 
        : base($"Workflow '{workflowId}' not found")
    {
        WorkflowId = workflowId;
    }
}

/// <summary>Workflow definition is invalid</summary>
public sealed class InvalidWorkflowDefinitionException : WorkflowDomainException
{
    public string Details { get; }
    public InvalidWorkflowDefinitionException(string details)
        : base($"Workflow definition is invalid: {details}")
    {
        Details = details;
    }
}

/// <summary>Workflow instance not found</summary>
public sealed class WorkflowInstanceNotFoundException : WorkflowDomainException
{
    public Guid InstanceId { get; }
    public WorkflowInstanceNotFoundException(Guid instanceId)
        : base($"Workflow instance '{instanceId}' not found")
    {
        InstanceId = instanceId;
    }
}

/// <summary>Invalid workflow instance state transition</summary>
public sealed class InvalidTransitionException : WorkflowDomainException
{
    public Guid InstanceId { get; }
    public string FromState { get; }
    public string ToState { get; }

    public InvalidTransitionException(Guid instanceId, string fromState, string toState)
        : base($"Cannot transition from '{fromState}' to '{toState}' for instance '{instanceId}'")
    {
        InstanceId = instanceId;
        FromState = fromState;
        ToState = toState;
    }
}

/// <summary>Approval task not found</summary>
public sealed class ApprovalTaskNotFoundException : WorkflowDomainException
{
    public Guid TaskId { get; }
    public ApprovalTaskNotFoundException(Guid taskId)
        : base($"Approval task '{taskId}' not found")
    {
        TaskId = taskId;
    }
}

/// <summary>Approval not permitted</summary>
public sealed class ApprovalNotPermittedException : WorkflowDomainException
{
    public Guid TaskId { get; }
    public Guid UserId { get; }

    public ApprovalNotPermittedException(Guid taskId, Guid userId)
        : base($"User '{userId}' is not permitted to approve task '{taskId}'")
    {
        TaskId = taskId;
        UserId = userId;
    }
}

/// <summary>Invalid workflow version</summary>
public sealed class InvalidWorkflowVersionException : WorkflowDomainException
{
    public int RequestedVersion { get; }
    public int AvailableVersion { get; }

    public InvalidWorkflowVersionException(int requested, int available)
        : base($"Workflow version {requested} not available. Current version is {available}")
    {
        RequestedVersion = requested;
        AvailableVersion = available;
    }
}

/// <summary>Workflow state not found</summary>
public sealed class WorkflowStateNotFoundException : WorkflowDomainException
{
    public Guid StateId { get; }
    public WorkflowStateNotFoundException(Guid stateId)
        : base($"Workflow state '{stateId}' not found")
    {
        StateId = stateId;
    }
}

/// <summary>Cannot perform action on workflow in current state</summary>
public sealed class InvalidWorkflowStateException : WorkflowDomainException
{
    public Guid InstanceId { get; }
    public InstanceStatus CurrentStatus { get; }

    public InvalidWorkflowStateException(Guid instanceId, InstanceStatus currentStatus, string action)
        : base($"Cannot {action} workflow instance '{instanceId}' while in '{currentStatus}' state")
    {
        InstanceId = instanceId;
        CurrentStatus = currentStatus;
    }
}

/// <summary>Guard evaluation failed</summary>
public sealed class GuardEvaluationException : WorkflowDomainException
{
    public string VariableName { get; }
    public GuardEvaluationException(string variableName, string reason)
        : base($"Failed to evaluate guard for variable '{variableName}': {reason}")
    {
        VariableName = variableName;
    }
}

/// <summary>Circular dependency detected in workflow</summary>
public sealed class CircularDependencyException : WorkflowDomainException
{
    public CircularDependencyException(string description)
        : base($"Circular dependency detected in workflow: {description}")
    {
    }
}

/// <summary>Required variable missing during execution</summary>
public sealed class MissingRequiredVariableException : WorkflowDomainException
{
    public string VariableName { get; }
    public Guid WorkflowInstanceId { get; }

    public MissingRequiredVariableException(Guid instanceId, string variableName)
        : base($"Required variable '{variableName}' is missing in workflow instance '{instanceId}'")
    {
        WorkflowInstanceId = instanceId;
        VariableName = variableName;
    }
}

/// <summary>Approval deadline passed</summary>
public sealed class ApprovalDeadlineExpiredException : WorkflowDomainException
{
    public Guid TaskId { get; }
    public DateTime DeadlineAt { get; }

    public ApprovalDeadlineExpiredException(Guid taskId, DateTime deadline)
        : base($"Approval task '{taskId}' deadline passed ({deadline:O})")
    {
        TaskId = taskId;
        DeadlineAt = deadline;
    }
}

/// <summary>Cannot publish workflow due to validation errors</summary>
public sealed class CannotPublishWorkflowException : WorkflowDomainException
{
    public List<string> ValidationErrors { get; }

    public CannotPublishWorkflowException(List<string> errors)
        : base($"Cannot publish workflow: {string.Join("; ", errors)}")
    {
        ValidationErrors = errors;
    }
}
