using Edp.SharedKernel.Domain;
using Edp.SharedKernel.Entities;

namespace Edp.Workflow.Domain;

/// <summary>
/// Workflow entity - represents a workflow definition (immutable once published)
/// </summary>
public class Workflow : AuditableEntity<Guid>
{
    /// <summary>Organization this workflow belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Unique workflow code within organization</summary>
    public string Code { get; set; } = null!;

    /// <summary>Display name</summary>
    public string Name { get; set; } = null!;

    /// <summary>Long description</summary>
    public string? Description { get; set; }

    /// <summary>Current workflow status</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    /// <summary>Latest version number</summary>
    public int LatestVersion { get; set; } = 0;

    /// <summary>Published version (null if never published)</summary>
    public int? PublishedVersion { get; set; }

    /// <summary>All versions of this workflow</summary>
    private List<WorkflowVersion> _versions = new();
    public IReadOnlyList<WorkflowVersion> Versions => _versions.AsReadOnly();

    public Workflow() { }

    public Workflow(Guid organizationId, string code, string name, string? description = null)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        Description = description;
        Status = WorkflowStatus.Draft;
        LatestVersion = 0;
    }

    /// <summary>Create a new version of this workflow</summary>
    public WorkflowVersion CreateNewVersion(Guid createdBy)
    {
        // Only Draft status allows creating new versions
        if (Status != WorkflowStatus.Draft)
            throw new InvalidWorkflowStateException(Id, InstanceStatus.Pending, "create new version");

        LatestVersion++;
        
        var version = new WorkflowVersion(
            Id,
            LatestVersion,
            createdBy,
            OrganizationId
        );

        _versions.Add(version);
        return version;
    }

    /// <summary>Publish the current version</summary>
    public void PublishVersion(int version, Guid publishedBy)
    {
        if (Status == WorkflowStatus.Archived)
            throw new InvalidOperationException("Cannot publish archived workflow");

        var versionToPublish = _versions.FirstOrDefault(v => v.Version == version)
            ?? throw new InvalidWorkflowVersionException(version, LatestVersion);

        // Validate version before publishing
        if (!versionToPublish.IsValid())
            throw new CannotPublishWorkflowException(versionToPublish.GetValidationErrors());

        Status = WorkflowStatus.Published;
        PublishedVersion = version;
        ModifiedBy = publishedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Archive this workflow (no new instances can be created)</summary>
    public void Archive(Guid archivedBy)
    {
        Status = WorkflowStatus.Archived;
        ModifiedBy = archivedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Suspend this workflow temporarily</summary>
    public void Suspend(Guid suspendedBy)
    {
        if (Status == WorkflowStatus.Archived)
            throw new InvalidOperationException("Cannot suspend archived workflow");

        Status = WorkflowStatus.Suspended;
        ModifiedBy = suspendedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Resume a suspended workflow</summary>
    public void Resume(Guid resumedBy)
    {
        if (Status != WorkflowStatus.Suspended)
            throw new InvalidOperationException("Only suspended workflows can be resumed");

        Status = WorkflowStatus.Published;
        ModifiedBy = resumedBy.ToString();
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Get a specific version</summary>
    public WorkflowVersion GetVersion(int version)
    {
        return _versions.FirstOrDefault(v => v.Version == version)
            ?? throw new InvalidWorkflowVersionException(version, LatestVersion);
    }

    /// <summary>Get the published version (if any)</summary>
    public WorkflowVersion? GetPublishedVersion()
    {
        if (!PublishedVersion.HasValue)
            return null;

        return GetVersion(PublishedVersion.Value);
    }

}

/// <summary>
/// Workflow version - immutable snapshot of workflow definition at a point in time
/// </summary>
public class WorkflowVersion : AuditableEntity<Guid>
{
    /// <summary>Parent workflow ID</summary>
    public Guid WorkflowId { get; set; }

    /// <summary>Organization this version belongs to</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Version number</summary>
    public int Version { get; set; }

    /// <summary>Whether this version has been published</summary>
    public bool IsPublished { get; set; }

    /// <summary>All states in this version</summary>
    private List<WorkflowState> _states = new();
    public IReadOnlyList<WorkflowState> States => _states.AsReadOnly();

    /// <summary>All transitions in this version</summary>
    private List<WorkflowTransition> _transitions = new();
    public IReadOnlyList<WorkflowTransition> Transitions => _transitions.AsReadOnly();

    public void LoadDefinition(IEnumerable<WorkflowState> states, IEnumerable<WorkflowTransition> transitions)
    {
        _states = states.ToList();
        _transitions = transitions.ToList();
    }

    /// <summary>Start state ID</summary>
    public Guid StartStateId { get; set; }

    public WorkflowVersion() { }

    public WorkflowVersion(Guid workflowId, int version, Guid createdBy, Guid organizationId)
    {
        Id = Guid.NewGuid();
        WorkflowId = workflowId;
        Version = version;
        OrganizationId = organizationId;
        IsPublished = false;
        CreatedBy = createdBy.ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Add a state to this version</summary>
    public WorkflowState AddState(
        string stateName,
        StateType stateType,
        Dictionary<string, string>? configuration = null)
    {
        // Validate state name uniqueness
        if (_states.Any(s => s.Name == stateName))
            throw new InvalidOperationException($"State '{stateName}' already exists in this version");

        var state = new WorkflowState(
            Id,
            stateName,
            stateType,
            OrganizationId,
            configuration
        );

        // Set first state as start state
        if (_states.Count == 0 && stateType == StateType.Start)
            StartStateId = state.Id;

        _states.Add(state);
        return state;
    }

    /// <summary>Add a transition between states</summary>
    public WorkflowTransition AddTransition(
        Guid fromStateId,
        Guid toStateId,
        TransitionGuard? guard = null,
        int order = 0)
    {
        // Validate states exist
        if (!_states.Any(s => s.Id == fromStateId))
            throw new WorkflowStateNotFoundException(fromStateId);

        if (!_states.Any(s => s.Id == toStateId))
            throw new WorkflowStateNotFoundException(toStateId);

        // Check for circular dependencies (simple cycle detection)
        if (WouldCreateCycle(fromStateId, toStateId))
            throw new CircularDependencyException($"Transition would create cycle between states");

        var transition = new WorkflowTransition(
            Id,
            fromStateId,
            toStateId,
            OrganizationId,
            guard,
            order
        );

        _transitions.Add(transition);
        return transition;
    }

    /// <summary>Check if adding this transition would create a cycle</summary>
    private bool WouldCreateCycle(Guid fromStateId, Guid toStateId)
    {
        // Simple cycle detection: from toState, can we reach fromState via existing transitions?
        var visited = new HashSet<Guid>();
        return CanReach(toStateId, fromStateId, visited);
    }

    private bool CanReach(Guid currentId, Guid targetId, HashSet<Guid> visited)
    {
        if (currentId == targetId)
            return true;

        if (visited.Contains(currentId))
            return false;

        visited.Add(currentId);

        var outgoingTransitions = _transitions.Where(t => t.FromStateId == currentId);
        foreach (var transition in outgoingTransitions)
        {
            if (CanReach(transition.ToStateId, targetId, visited))
                return true;
        }

        return false;
    }

    /// <summary>Validate this workflow version</summary>
    public bool IsValid()
    {
        var errors = GetValidationErrors();
        return errors.Count == 0;
    }

    /// <summary>Get validation errors for this version</summary>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        // Must have at least one state
        if (_states.Count == 0)
            errors.Add("Workflow must have at least one state");

        // Must have a start state
        if (!_states.Any(s => s.StateType == StateType.Start))
            errors.Add("Workflow must have a Start state");

        // Must have an end state
        if (!_states.Any(s => s.StateType == StateType.End))
            errors.Add("Workflow must have an End state");

        // Validate all states are reachable from start
        var unreachable = GetUnreachableStates();
        if (unreachable.Any())
            errors.Add($"Unreachable states: {string.Join(", ", unreachable.Select(s => s.Name))}");

        // Validate transitions
        var invalidTransitions = ValidateTransitions();
        errors.AddRange(invalidTransitions);

        return errors;
    }

    /// <summary>Find states that cannot be reached from the start state</summary>
    private List<WorkflowState> GetUnreachableStates()
    {
        if (_states.Count == 0)
            return new List<WorkflowState>();

        var startState = _states.FirstOrDefault(s => s.StateType == StateType.Start);
        if (startState == null)
            return _states;

        var reachable = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(startState.Id);
        reachable.Add(startState.Id);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var outgoing = _transitions.Where(t => t.FromStateId == currentId);

            foreach (var transition in outgoing)
            {
                if (!reachable.Contains(transition.ToStateId))
                {
                    reachable.Add(transition.ToStateId);
                    queue.Enqueue(transition.ToStateId);
                }
            }
        }

        return _states.Where(s => !reachable.Contains(s.Id)).ToList();
    }

    /// <summary>Validate all transitions</summary>
    private List<string> ValidateTransitions()
    {
        var errors = new List<string>();

        foreach (var transition in _transitions)
        {
            var fromState = _states.FirstOrDefault(s => s.Id == transition.FromStateId);
            var toState = _states.FirstOrDefault(s => s.Id == transition.ToStateId);

            if (fromState == null)
                errors.Add($"Transition references non-existent from state '{transition.FromStateId}'");

            if (toState == null)
                errors.Add($"Transition references non-existent to state '{transition.ToStateId}'");

            // Validate guard if present
            var guard = transition.GetGuard();
            if (guard != null && !guard.IsValid())
                errors.Add($"Transition has invalid guard (too deeply nested)");
        }

        return errors;
    }

    /// <summary>Get state by ID</summary>
    public WorkflowState? GetState(Guid stateId) => _states.FirstOrDefault(s => s.Id == stateId);

    /// <summary>Get transitions from a state</summary>
    public IReadOnlyList<WorkflowTransition> GetTransitionsFrom(Guid stateId) =>
        _transitions.Where(t => t.FromStateId == stateId).ToList().AsReadOnly();
}
