using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Application.Contracts;
using Edp.Workflow.Domain;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Workflow.Infrastructure.Persistence;

public sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly EdpDbContext _dbContext;

    public WorkflowRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<global::Edp.Workflow.Domain.Workflow?> GetByIdAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken = default) =>
        _dbContext.Workflows.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == workflowId, cancellationToken);

    public Task<global::Edp.Workflow.Domain.Workflow?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default) =>
        _dbContext.Workflows.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Code == code, cancellationToken);

    public async Task<IReadOnlyList<global::Edp.Workflow.Domain.Workflow>> ListAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await _dbContext.Workflows.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<global::Edp.Workflow.Domain.Workflow>> ListPageAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await _dbContext.Workflows
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(global::Edp.Workflow.Domain.Workflow workflow, CancellationToken cancellationToken = default)
    {
        await _dbContext.Workflows.AddAsync(workflow, cancellationToken);
    }

    public async Task UpdateAsync(global::Edp.Workflow.Domain.Workflow workflow, CancellationToken cancellationToken = default)
    {
        _dbContext.Workflows.Update(workflow);
    }
}

public sealed class WorkflowVersionRepository : IWorkflowVersionRepository
{
    private readonly EdpDbContext _dbContext;

    public WorkflowVersionRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<WorkflowVersion?> GetByIdAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowVersions.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == versionId, cancellationToken);

    public Task<WorkflowVersion?> GetAsync(Guid organizationId, Guid workflowId, int version, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowVersions.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.WorkflowId == workflowId && x.Version == version, cancellationToken);

    public async Task<WorkflowVersion?> GetForExecutionAsync(Guid organizationId, Guid workflowId, int version, CancellationToken cancellationToken = default)
    {
        var workflowVersion = await GetAsync(organizationId, workflowId, version, cancellationToken);
        if (workflowVersion is null)
            return null;

        var states = await _dbContext.WorkflowStates
            .Where(state => state.OrganizationId == organizationId && state.WorkflowVersionId == workflowVersion.Id)
            .ToListAsync(cancellationToken);
        var transitions = await _dbContext.WorkflowTransitions
            .Where(transition => transition.OrganizationId == organizationId && transition.WorkflowVersionId == workflowVersion.Id)
            .OrderBy(transition => transition.Order)
            .ToListAsync(cancellationToken);
        workflowVersion.LoadDefinition(states, transitions);
        return workflowVersion;
    }

    public async Task<IReadOnlyList<WorkflowVersion>> ListAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowVersions.Where(x => x.OrganizationId == organizationId && x.WorkflowId == workflowId).OrderByDescending(x => x.Version).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowVersion>> ListPageAsync(Guid organizationId, Guid workflowId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowVersions
            .Where(x => x.OrganizationId == organizationId && x.WorkflowId == workflowId)
            .OrderByDescending(x => x.Version)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkflowVersion version, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkflowVersions.AddAsync(version, cancellationToken);
    }

    public async Task UpdateAsync(WorkflowVersion version, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowVersions.Update(version);
    }
}

public sealed class WorkflowStateRepository : IWorkflowStateRepository
{
    private readonly EdpDbContext _dbContext;

    public WorkflowStateRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<WorkflowState?> GetByIdAsync(Guid organizationId, Guid stateId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowStates.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == stateId, cancellationToken);

    public async Task<IReadOnlyList<WorkflowState>> ListAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowStates
            .Where(x => x.OrganizationId == organizationId && x.WorkflowVersionId == versionId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkflowStates.AddAsync(state, cancellationToken);
    }
}

public sealed class WorkflowTransitionRepository : IWorkflowTransitionRepository
{
    private readonly EdpDbContext _dbContext;

    public WorkflowTransitionRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<WorkflowTransition>> ListAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowTransitions
            .Where(x => x.OrganizationId == organizationId && x.WorkflowVersionId == versionId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkflowTransition transition, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkflowTransitions.AddAsync(transition, cancellationToken);
    }
}

public sealed class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly EdpDbContext _dbContext;

    public WorkflowInstanceRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<WorkflowInstance?> GetByIdAsync(Guid organizationId, Guid instanceId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == instanceId, cancellationToken);

    public async Task<IReadOnlyList<WorkflowInstance>> ListAsync(Guid organizationId, InstanceStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkflowInstances.Where(x => x.OrganizationId == organizationId);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkflowInstances.AddAsync(instance, cancellationToken);
    }

    public async Task UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkflowInstances.Update(instance);
    }
}

public sealed class ApprovalTaskRepository : IApprovalTaskRepository
{
    private readonly EdpDbContext _dbContext;

    public ApprovalTaskRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<ApprovalTask?> GetByIdAsync(Guid organizationId, Guid taskId, CancellationToken cancellationToken = default) =>
        _dbContext.ApprovalTasks.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == taskId, cancellationToken);

    public async Task<IReadOnlyList<ApprovalTask>> ListByInstanceAsync(Guid organizationId, Guid instanceId, Guid? stateId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ApprovalTasks
            .Where(x => x.OrganizationId == organizationId && x.WorkflowInstanceId == instanceId);
        if (stateId.HasValue)
            query = query.Where(x => x.StateId == stateId.Value);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalTask>> ListForUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default) =>
        await _dbContext.ApprovalTasks
            .Where(x => x.OrganizationId == organizationId && x.AssignedToUserId == userId && x.Status == ApprovalStatus.Pending)
            .OrderBy(x => x.DeadlineAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApprovalTask>> ListForUserPageAsync(Guid organizationId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await _dbContext.ApprovalTasks
            .Where(x => x.OrganizationId == organizationId && x.AssignedToUserId == userId && x.Status == ApprovalStatus.Pending)
            .OrderBy(x => x.DeadlineAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApprovalTask>> ListExpiredAsync(DateTime nowUtc, int batchSize, CancellationToken cancellationToken = default) =>
        await _dbContext.ApprovalTasks
            .Where(x => x.Status == ApprovalStatus.Pending && x.DeadlineAt.HasValue && x.DeadlineAt.Value < nowUtc)
            .OrderBy(x => x.DeadlineAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ApprovalTask task, CancellationToken cancellationToken = default) =>
        await _dbContext.ApprovalTasks.AddAsync(task, cancellationToken);

    public async Task UpdateAsync(ApprovalTask task, CancellationToken cancellationToken = default)
    {
        _dbContext.ApprovalTasks.Update(task);
    }
}

public sealed class WorkflowHistoryRepository : IWorkflowHistoryRepository
{
    private readonly EdpDbContext _dbContext;

    public WorkflowHistoryRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(WorkflowHistory history, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowHistory.AddAsync(history, cancellationToken);

    public async Task<IReadOnlyList<WorkflowHistory>> ListAsync(Guid organizationId, Guid instanceId, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowHistory
            .Where(x => x.OrganizationId == organizationId && x.WorkflowInstanceId == instanceId)
            .OrderBy(x => x.EventAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowHistory>> ListPageAsync(Guid organizationId, Guid instanceId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowHistory
            .Where(x => x.OrganizationId == organizationId && x.WorkflowInstanceId == instanceId)
            .OrderBy(x => x.EventAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}

public sealed class IdempotencyRepository : IIdempotencyRepository
{
    private readonly EdpDbContext _dbContext;
    public IdempotencyRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<IdempotencyRecord?> GetAsync(Guid organizationId, string key, CancellationToken cancellationToken = default) =>
        _dbContext.IdempotencyRecords.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Key == key, cancellationToken);

    public async Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default) =>
        await _dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);
}

public sealed class InboxMessageRepository : IInboxMessageRepository
{
    private readonly EdpDbContext _dbContext;
    public InboxMessageRepository(EdpDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default) =>
        _dbContext.InboxMessages.AnyAsync(x => x.MessageId == messageId, cancellationToken);

    public async Task AddAsync(InboxMessage message, CancellationToken cancellationToken = default) =>
        await _dbContext.InboxMessages.AddAsync(message, cancellationToken);

    public async Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.InboxMessages.FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
        message?.MarkProcessed();
    }
}
