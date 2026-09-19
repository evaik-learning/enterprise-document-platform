using Edp.Workflow.Domain;

namespace Edp.Workflow.Application.Interfaces;

public interface IWorkflowDefinitionService
{
    Task<global::Edp.Workflow.Domain.Workflow> CreateWorkflowAsync(Guid organizationId, string code, string name, string? description, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<WorkflowVersion> CreateVersionAsync(Guid organizationId, Guid workflowId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowVersion>> GetVersionsAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowVersion>> GetVersionsPageAsync(Guid organizationId, Guid workflowId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WorkflowState> AddStateAsync(Guid organizationId, Guid versionId, string name, StateType stateType, Dictionary<string, string>? configuration, CancellationToken cancellationToken = default);
    Task<WorkflowTransition> AddTransitionAsync(Guid organizationId, Guid versionId, Guid fromStateId, Guid toStateId, TransitionGuard? guard, int order, string triggerType = "complete", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ValidateVersionAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken = default);
    Task PublishVersionAsync(Guid organizationId, Guid workflowId, int version, Guid actorUserId, CancellationToken cancellationToken = default);
    Task ArchiveWorkflowAsync(Guid organizationId, Guid workflowId, Guid actorUserId, CancellationToken cancellationToken = default);
}
