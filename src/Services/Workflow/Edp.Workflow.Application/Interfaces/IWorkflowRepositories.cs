using Edp.Workflow.Domain;

namespace Edp.Workflow.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<global::Edp.Workflow.Domain.Workflow?> GetByIdAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken = default);
    Task<global::Edp.Workflow.Domain.Workflow?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Workflow.Domain.Workflow>> ListAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Workflow.Domain.Workflow>> ListPageAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(global::Edp.Workflow.Domain.Workflow workflow, CancellationToken cancellationToken = default);
    Task UpdateAsync(global::Edp.Workflow.Domain.Workflow workflow, CancellationToken cancellationToken = default);
}

public interface IWorkflowVersionRepository
{
    Task<WorkflowVersion?> GetByIdAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken = default);
    Task<WorkflowVersion?> GetAsync(Guid organizationId, Guid workflowId, int version, CancellationToken cancellationToken = default);
    Task<WorkflowVersion?> GetForExecutionAsync(Guid organizationId, Guid workflowId, int version, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowVersion>> ListAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowVersion>> ListPageAsync(Guid organizationId, Guid workflowId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowVersion version, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowVersion version, CancellationToken cancellationToken = default);
}

public interface IWorkflowStateRepository
{
    Task<WorkflowState?> GetByIdAsync(Guid organizationId, Guid stateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowState>> ListAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowState state, CancellationToken cancellationToken = default);
}

public interface IWorkflowTransitionRepository
{
    Task<IReadOnlyList<WorkflowTransition>> ListAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowTransition transition, CancellationToken cancellationToken = default);
}

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(Guid organizationId, Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowInstance>> ListAsync(Guid organizationId, InstanceStatus? status = null, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
}

public interface IApprovalTaskRepository
{
    Task<ApprovalTask?> GetByIdAsync(Guid organizationId, Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalTask>> ListByInstanceAsync(Guid organizationId, Guid instanceId, Guid? stateId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalTask>> ListForUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalTask>> ListForUserPageAsync(Guid organizationId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalTask>> ListExpiredAsync(DateTime nowUtc, int batchSize, CancellationToken cancellationToken = default);
    Task AddAsync(ApprovalTask task, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApprovalTask task, CancellationToken cancellationToken = default);
}

public interface IWorkflowHistoryRepository
{
    Task AddAsync(WorkflowHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowHistory>> ListAsync(Guid organizationId, Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowHistory>> ListPageAsync(Guid organizationId, Guid instanceId, int page, int pageSize, CancellationToken cancellationToken = default);
}
