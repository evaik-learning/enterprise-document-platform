using Edp.SharedKernel.Domain;
using Edp.SharedKernel.Entities;

namespace Edp.Workflow.Domain;

/// <summary>
/// Workflow state - defines a state in the workflow
/// </summary>
public class WorkflowState : AuditableEntity<Guid>
{
    /// <summary>Workflow version this state belongs to</summary>
    public Guid WorkflowVersionId { get; set; }

    /// <summary>Organization this state belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>State name</summary>
    public string Name { get; set; } = null!;

    /// <summary>Type of state</summary>
    public StateType StateType { get; set; }

    /// <summary>Display label for the state</summary>
    public string? Label { get; set; }

    /// <summary>State configuration (JSON-serialized)</summary>
    public string? Configuration { get; set; }

    /// <summary>Approval policy for this state (if approval state)</summary>
    public string? ApprovalPolicyJson { get; set; }

    /// <summary>Persisted approver assignment rules for approval states.</summary>
    public string? AssignmentRulesJson { get; set; }

    /// <summary>Assignment rules for approvers</summary>
    private List<AssignmentRule> _assignmentRules = new();
    public IReadOnlyList<AssignmentRule> AssignmentRules => _assignmentRules.AsReadOnly();

    /// <summary>Required variables for this state</summary>
    private List<string> _requiredVariables = new();
    public IReadOnlyList<string> RequiredVariables => _requiredVariables.AsReadOnly();

    public WorkflowState() { }

    public WorkflowState(
        Guid workflowVersionId,
        string name,
        StateType stateType,
        Guid organizationId,
        Dictionary<string, string>? configuration = null)
    {
        Id = Guid.NewGuid();
        WorkflowVersionId = workflowVersionId;
        OrganizationId = organizationId;
        Name = name;
        StateType = stateType;
        Label = name;

        if (configuration != null)
            Configuration = System.Text.Json.JsonSerializer.Serialize(configuration);
    }

    /// <summary>Add assignment rule for approvers</summary>
    public void AddAssignmentRule(AssignmentRule rule)
    {
        if (StateType != StateType.Approval && StateType != StateType.ParallelApprovalAll && StateType != StateType.ParallelApprovalAny)
            throw new InvalidOperationException("Assignment rules can only be added to approval states");

        if (!_assignmentRules.Contains(rule))
            _assignmentRules.Add(rule);
        AssignmentRulesJson = System.Text.Json.JsonSerializer.Serialize(_assignmentRules);
    }

    public void SetAssignmentRules(IEnumerable<AssignmentRule> rules)
    {
        _assignmentRules = rules.Distinct().ToList();
        AssignmentRulesJson = System.Text.Json.JsonSerializer.Serialize(_assignmentRules);
    }

    public void LoadPersistedAssignmentRules()
    {
        if (string.IsNullOrWhiteSpace(AssignmentRulesJson))
            return;
        _assignmentRules = System.Text.Json.JsonSerializer.Deserialize<List<AssignmentRule>>(AssignmentRulesJson) ?? [];
    }

    /// <summary>Add required variable</summary>
    public void AddRequiredVariable(string variableName)
    {
        if (!_requiredVariables.Contains(variableName))
            _requiredVariables.Add(variableName);
    }

    /// <summary>Set approval policy</summary>
    public void SetApprovalPolicy(ApprovalPolicy policy)
    {
        if (StateType != StateType.Approval && StateType != StateType.ParallelApprovalAll && StateType != StateType.ParallelApprovalAny)
            throw new InvalidOperationException("Approval policy can only be set on approval states");

        ApprovalPolicyJson = System.Text.Json.JsonSerializer.Serialize(policy);
    }

    /// <summary>Get approval policy</summary>
    public ApprovalPolicy? GetApprovalPolicy()
    {
        if (string.IsNullOrEmpty(ApprovalPolicyJson))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ApprovalPolicy>(ApprovalPolicyJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Get configuration as dictionary</summary>
    public Dictionary<string, string>? GetConfiguration()
    {
        if (string.IsNullOrEmpty(Configuration))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Configuration);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Workflow transition - defines a transition between two states
/// </summary>
public class WorkflowTransition : AuditableEntity<Guid>
{
    /// <summary>Workflow version this transition belongs to</summary>
    public Guid WorkflowVersionId { get; set; }

    /// <summary>Organization this transition belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Source state ID</summary>
    public Guid FromStateId { get; set; }

    /// <summary>Destination state ID</summary>
    public Guid ToStateId { get; set; }

    /// <summary>Guard condition for this transition (null = always valid)</summary>
    public string? GuardJson { get; set; }

    /// <summary>Execution order if multiple transitions exist from same state</summary>
    public int Order { get; set; }

    /// <summary>Display label for the transition</summary>
    public string? Label { get; set; }

    public string TriggerType { get; set; } = "complete";

    public WorkflowTransition() { }

    public WorkflowTransition(
        Guid workflowVersionId,
        Guid fromStateId,
        Guid toStateId,
        Guid organizationId,
        TransitionGuard? guard = null,
        int order = 0,
        string triggerType = "complete")
    {
        Id = Guid.NewGuid();
        WorkflowVersionId = workflowVersionId;
        OrganizationId = organizationId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        Order = order;
        TriggerType = string.IsNullOrWhiteSpace(triggerType) ? "complete" : triggerType;

        if (guard != null)
            GuardJson = System.Text.Json.JsonSerializer.Serialize(guard);
    }

    /// <summary>Get guard condition</summary>
    public TransitionGuard? GetGuard()
    {
        if (string.IsNullOrEmpty(GuardJson))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<TransitionGuard>(GuardJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Set guard condition</summary>
    public void SetGuard(TransitionGuard? guard)
    {
        if (guard == null)
        {
            GuardJson = null;
        }
        else
        {
            if (!guard.IsValid())
                throw new InvalidOperationException("Guard contains invalid conditions (too deeply nested)");

            GuardJson = System.Text.Json.JsonSerializer.Serialize(guard);
        }
    }
}
