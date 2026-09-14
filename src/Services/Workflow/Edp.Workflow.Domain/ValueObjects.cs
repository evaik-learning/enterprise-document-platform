namespace Edp.Workflow.Domain;

/// <summary>
/// Workflow code - unique identifier for workflow definitions within an organization
/// </summary>
public record WorkflowCode
{
    public string Value { get; }

    public WorkflowCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Workflow code cannot be empty", nameof(value));
        if (value.Length > 50)
            throw new ArgumentException("Workflow code cannot exceed 50 characters", nameof(value));
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Z][A-Z0-9_]*$"))
            throw new ArgumentException("Workflow code must start with letter and contain only uppercase letters, digits, and underscores", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Approval policy configuration
/// </summary>
public record ApprovalPolicy(
    ApprovalPolicyType Type,
    List<Guid> ApproverUserIds,
    Dictionary<string, string>? CustomConfiguration = null
)
{
    public ApprovalPolicy(ApprovalPolicyType type, List<Guid> approverUserIds)
        : this(type, approverUserIds, null)
    {
        if (approverUserIds == null || approverUserIds.Count == 0)
            throw new ArgumentException("Approver list cannot be empty", nameof(approverUserIds));
    }
}

/// <summary>
/// Guard condition - reusable condition evaluation
/// </summary>
public record GuardCondition(
    string Variable,
    GuardOperator Operator,
    object? ExpectedValue = null
);

/// <summary>
/// Assignment rule - determines who can approve
/// </summary>
public record AssignmentRule(
    AssignmentRuleType Type,
    List<Guid>? UserIds = null,
    string? RoleCode = null,
    string? VariableName = null
)
{
    public AssignmentRule(AssignmentRuleType type, List<Guid>? userIds)
        : this(type, userIds, null, null)
    {
        if (type == AssignmentRuleType.FixedUsers && (userIds == null || userIds.Count == 0))
            throw new ArgumentException("FixedUsers rule requires user IDs", nameof(userIds));
    }

    public AssignmentRule(AssignmentRuleType type, string roleCode)
        : this(type, null, roleCode, null)
    {
        if (type == AssignmentRuleType.OrganizationRole && string.IsNullOrWhiteSpace(roleCode))
            throw new ArgumentException("OrganizationRole rule requires role code", nameof(roleCode));
    }

    public AssignmentRule(AssignmentRuleType type, string variableName, bool _)
        : this(type, null, null, variableName)
    {
        if (type == AssignmentRuleType.Variable && string.IsNullOrWhiteSpace(variableName))
            throw new ArgumentException("Variable rule requires variable name", nameof(variableName));
    }
}

/// <summary>
/// Transition guard - condition tree for state transitions
/// </summary>
public record TransitionGuard(
    GuardCondition? Condition = null,
    GuardLogicalOperator LogicalOperator = GuardLogicalOperator.And,
    List<TransitionGuard>? Children = null
)
{
    /// <summary>
    /// Create a simple guard with a single condition
    /// </summary>
    public static TransitionGuard Single(GuardCondition condition) => 
        new(condition, GuardLogicalOperator.And, null);

    /// <summary>
    /// Create a compound guard with AND logic
    /// </summary>
    public static TransitionGuard All(params TransitionGuard[] guards) =>
        new(null, GuardLogicalOperator.And, guards.ToList());

    /// <summary>
    /// Create a compound guard with OR logic
    /// </summary>
    public static TransitionGuard Any(params TransitionGuard[] guards) =>
        new(null, GuardLogicalOperator.Or, guards.ToList());

    /// <summary>
    /// Validate guard depth (max 10 levels)
    /// </summary>
    public bool IsValid(int depth = 0)
    {
        if (depth > 10)
            return false;

        if (Children != null)
            return Children.All(c => c.IsValid(depth + 1));

        return true;
    }
}
