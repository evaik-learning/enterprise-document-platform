using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Domain;
using Edp.Shared.Infrastructure.Persistence;

namespace Edp.Workflow.Application.Services;

public sealed class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowVersionRepository _versionRepository;
    private readonly IWorkflowStateRepository _stateRepository;
    private readonly IWorkflowTransitionRepository _transitionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkflowDefinitionService(
        IWorkflowRepository workflowRepository,
        IWorkflowVersionRepository versionRepository,
        IWorkflowStateRepository stateRepository,
        IWorkflowTransitionRepository transitionRepository,
        IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _versionRepository = versionRepository;
        _stateRepository = stateRepository;
        _transitionRepository = transitionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<global::Edp.Workflow.Domain.Workflow> CreateWorkflowAsync(
        Guid organizationId,
        string code,
        string name,
        string? description,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (await _workflowRepository.GetByCodeAsync(organizationId, code, cancellationToken) is not null)
            throw new InvalidOperationException($"Workflow code '{code}' already exists.");

        var workflow = new global::Edp.Workflow.Domain.Workflow(organizationId, code, name, description)
        {
            CreatedBy = actorUserId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _workflowRepository.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return workflow;
    }

    public async Task<WorkflowVersion> CreateVersionAsync(
        Guid organizationId,
        Guid workflowId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowAsync(organizationId, workflowId, cancellationToken);
        if (workflow.Status == WorkflowStatus.Archived)
            throw new InvalidOperationException("Cannot create a version for an archived workflow.");

        var version = new WorkflowVersion(workflowId, workflow.LatestVersion + 1, actorUserId, organizationId);
        workflow.LatestVersion = version.Version;
        await _versionRepository.AddAsync(version, cancellationToken);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return version;
    }

    public async Task<IReadOnlyList<WorkflowVersion>> GetVersionsAsync(
        Guid organizationId,
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetWorkflowAsync(organizationId, workflowId, cancellationToken);
        return await _versionRepository.ListAsync(organizationId, workflowId, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowVersion>> GetVersionsPageAsync(
        Guid organizationId,
        Guid workflowId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _ = await GetWorkflowAsync(organizationId, workflowId, cancellationToken);
        return await _versionRepository.ListPageAsync(organizationId, workflowId, page, pageSize, cancellationToken);
    }

    public async Task<WorkflowState> AddStateAsync(
        Guid organizationId,
        Guid versionId,
        string name,
        StateType stateType,
        Dictionary<string, string>? configuration,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(organizationId, versionId, cancellationToken);
        EnsureDraft(version);

        var states = await _stateRepository.ListAsync(organizationId, versionId, cancellationToken);
        if (states.Any(state => string.Equals(state.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"State '{name}' already exists in this version.");
        if (stateType == StateType.Start && states.Any(state => state.StateType == StateType.Start))
            throw new InvalidOperationException("A workflow version can only have one start state.");

        var state = new WorkflowState(versionId, name, stateType, organizationId, configuration);
        await _stateRepository.AddAsync(state, cancellationToken);
        if (stateType == StateType.Start && version.StartStateId == Guid.Empty)
        {
            version.StartStateId = state.Id;
            await _versionRepository.UpdateAsync(version, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return state;
    }

    public async Task<WorkflowTransition> AddTransitionAsync(
        Guid organizationId,
        Guid versionId,
        Guid fromStateId,
        Guid toStateId,
        TransitionGuard? guard,
        int order,
        string triggerType = "complete",
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(organizationId, versionId, cancellationToken);
        EnsureDraft(version);
        var states = await _stateRepository.ListAsync(organizationId, versionId, cancellationToken);
        if (states.All(state => state.Id != fromStateId) || states.All(state => state.Id != toStateId))
            throw new WorkflowStateNotFoundException(states.All(state => state.Id != fromStateId) ? fromStateId : toStateId);

        var transitions = await _transitionRepository.ListAsync(organizationId, versionId, cancellationToken);
        var transition = new WorkflowTransition(versionId, fromStateId, toStateId, organizationId, guard, order, triggerType);
        if (transitions.Any(existing => existing.FromStateId == fromStateId && existing.ToStateId == toStateId && existing.Order == order))
            throw new InvalidOperationException("A transition with the same source, destination, and order already exists.");

        await _transitionRepository.AddAsync(transition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transition;
    }

    public async Task PublishVersionAsync(
        Guid organizationId,
        Guid workflowId,
        int version,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowAsync(organizationId, workflowId, cancellationToken);
        var versionDefinition = await GetVersionAsync(organizationId, workflowId, version, cancellationToken);
        EnsureDraft(versionDefinition);

        var states = await _stateRepository.ListAsync(organizationId, versionDefinition.Id, cancellationToken);
        var transitions = await _transitionRepository.ListAsync(organizationId, versionDefinition.Id, cancellationToken);
        versionDefinition.LoadDefinition(states, transitions);
        var validationErrors = versionDefinition.GetValidationErrors();
        if (validationErrors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", validationErrors));

        if (workflow.PublishedVersion is int publishedVersion && publishedVersion != version)
        {
            var previous = await GetVersionAsync(organizationId, workflowId, publishedVersion, cancellationToken);
            previous.IsPublished = false;
            await _versionRepository.UpdateAsync(previous, cancellationToken);
        }

        versionDefinition.IsPublished = true;
        await _versionRepository.UpdateAsync(versionDefinition, cancellationToken);
        workflow.Status = WorkflowStatus.Published;
        workflow.PublishedVersion = version;
        workflow.ModifiedBy = actorUserId.ToString();
        workflow.ModifiedAt = DateTimeOffset.UtcNow;
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveWorkflowAsync(
        Guid organizationId,
        Guid workflowId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowAsync(organizationId, workflowId, cancellationToken);
        workflow.Archive(actorUserId);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ValidateVersionAsync(
        Guid organizationId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(organizationId, versionId, cancellationToken);
        var states = await _stateRepository.ListAsync(organizationId, versionId, cancellationToken);
        var transitions = await _transitionRepository.ListAsync(organizationId, versionId, cancellationToken);
        version.LoadDefinition(states, transitions);
        return version.GetValidationErrors();
    }

    private async Task<global::Edp.Workflow.Domain.Workflow> GetWorkflowAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken) =>
        await _workflowRepository.GetByIdAsync(organizationId, workflowId, cancellationToken)
        ?? throw new WorkflowNotFoundException(workflowId);

    private async Task<WorkflowVersion> GetVersionAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken)
    {
        return await _versionRepository.GetByIdAsync(organizationId, versionId, cancellationToken)
            ?? throw new InvalidWorkflowVersionException(0, 0);
    }

    private async Task<WorkflowVersion> GetVersionAsync(Guid organizationId, Guid workflowId, int version, CancellationToken cancellationToken) =>
        await _versionRepository.GetAsync(organizationId, workflowId, version, cancellationToken)
        ?? throw new InvalidWorkflowVersionException(version, version);

    private static void EnsureDraft(WorkflowVersion version)
    {
        if (version.IsPublished)
            throw new InvalidOperationException("Published workflow versions are immutable.");
    }

}
