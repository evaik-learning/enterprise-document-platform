namespace Edp.Workflow.Domain;

/// <summary>
/// Workflow definition status
/// </summary>
public enum WorkflowStatus
{
    /// <summary>Draft, not yet published</summary>
    Draft = 0,
    
    /// <summary>Published and can be instantiated</summary>
    Published = 1,
    
    /// <summary>Archived, no longer instantiable</summary>
    Archived = 2,
    
    /// <summary>Suspended, temporarily disabled</summary>
    Suspended = 3
}

/// <summary>
/// Type of workflow state
/// </summary>
public enum StateType
{
    /// <summary>Initial entry point</summary>
    Start = 0,
    
    /// <summary>Regular task state</summary>
    Task = 1,
    
    /// <summary>Approval/decision state</summary>
    Approval = 2,
    
    /// <summary>Parallel approval (ALL must approve)</summary>
    ParallelApprovalAll = 3,
    
    /// <summary>Parallel approval (ANY can approve)</summary>
    ParallelApprovalAny = 4,
    
    /// <summary>Terminal/end state</summary>
    End = 5
}

/// <summary>
/// Workflow instance execution status
/// </summary>
public enum InstanceStatus
{
    /// <summary>Initiated, not yet started</summary>
    Pending = 0,
    
    /// <summary>Currently executing</summary>
    InProgress = 1,
    
    /// <summary>Waiting for approval/decision</summary>
    AwaitingApproval = 2,
    
    /// <summary>Successfully completed</summary>
    Completed = 3,
    
    /// <summary>Terminated by rejection</summary>
    Rejected = 4,
    
    /// <summary>Manually cancelled</summary>
    Cancelled = 5,
    
    /// <summary>Errored/failed</summary>
    Failed = 6,
    
    /// <summary>Temporarily paused</summary>
    Paused = 7
}

/// <summary>
/// Approval task status
/// </summary>
public enum ApprovalStatus
{
    /// <summary>Created, not yet assigned to approver</summary>
    Created = 0,
    
    /// <summary>Assigned and pending approver action</summary>
    Pending = 1,
    
    /// <summary>Approver has approved</summary>
    Approved = 2,
    
    /// <summary>Approver has rejected</summary>
    Rejected = 3,
    
    /// <summary>Reassigned to another approver</summary>
    Reassigned = 4,
    
    /// <summary>Cancelled (e.g., by workflow completion)</summary>
    Cancelled = 5,
    
    /// <summary>Expired (deadline passed)</summary>
    Expired = 6
}

/// <summary>
/// Type of approval policy
/// </summary>
public enum ApprovalPolicyType
{
    /// <summary>Tasks are created/approved sequentially</summary>
    Sequential = 0,
    
    /// <summary>All tasks must be approved before advancing</summary>
    ParallelAll = 1,
    
    /// <summary>Any task approval advances workflow</summary>
    ParallelAny = 2
}

/// <summary>
/// Guard condition operator
/// </summary>
public enum GuardOperator
{
    /// <summary>Variable equals value</summary>
    Equals = 0,
    
    /// <summary>Variable not equals value</summary>
    NotEquals = 1,
    
    /// <summary>Numeric greater than</summary>
    GreaterThan = 2,
    
    /// <summary>Numeric less than</summary>
    LessThan = 3,
    
    /// <summary>Numeric greater than or equals</summary>
    GreaterThanOrEquals = 4,
    
    /// <summary>Numeric less than or equals</summary>
    LessThanOrEquals = 5,
    
    /// <summary>String contains</summary>
    Contains = 6,
    
    /// <summary>String starts with</summary>
    StartsWith = 7,
    
    /// <summary>String ends with</summary>
    EndsWith = 8,
    
    /// <summary>Variable is null</summary>
    IsNull = 9,
    
    /// <summary>Variable is not null</summary>
    IsNotNull = 10
}

/// <summary>
/// Guard condition logical operator
/// </summary>
public enum GuardLogicalOperator
{
    /// <summary>All conditions must be true</summary>
    And = 0,
    
    /// <summary>At least one condition must be true</summary>
    Or = 1
}

/// <summary>
/// Assignment rule type
/// </summary>
public enum AssignmentRuleType
{
    /// <summary>Fixed list of users</summary>
    FixedUsers = 0,
    
    /// <summary>Users from organization role</summary>
    OrganizationRole = 1,
    
    /// <summary>Users from variable</summary>
    Variable = 2,
    
    /// <summary>User from document creator</summary>
    DocumentCreator = 3,
    
    /// <summary>User from document owner</summary>
    DocumentOwner = 4
}

/// <summary>
/// History event type
/// </summary>
public enum HistoryEventType
{
    InstanceCreated = 0,
    InstanceStarted = 1,
    StateEntered = 2,
    StateExited = 3,
    ApprovalAssigned = 4,
    ApprovalApproved = 5,
    ApprovalRejected = 6,
    ApprovalExpired = 13,
    ApprovalReassigned = 7,
    TransitionEvaluated = 8,
    InstanceCompleted = 9,
    InstanceRejected = 10,
    InstanceCancelled = 11,
    InstanceFailed = 12
}
