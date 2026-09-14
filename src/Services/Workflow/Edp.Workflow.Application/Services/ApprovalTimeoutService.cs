using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Application.Contracts;
using Edp.Workflow.Domain;
using Edp.Shared.Contracts;
using Edp.Shared.Infrastructure.Persistence;

namespace Edp.Workflow.Application.Services;

public sealed class ApprovalTimeoutService : IApprovalTimeoutService
{
    private readonly IApprovalTaskRepository _approvalTaskRepository;
    private readonly IWorkflowHistoryRepository _historyRepository;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApprovalTimeoutService(
        IApprovalTaskRepository approvalTaskRepository,
        IWorkflowHistoryRepository historyRepository,
        IOutboxMessageRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _approvalTaskRepository = approvalTaskRepository;
        _historyRepository = historyRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ProcessExpiredAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var tasks = await _approvalTaskRepository.ListExpiredAsync(DateTime.UtcNow, batchSize, cancellationToken);
        foreach (var task in tasks)
        {
            task.MarkExpiredIfNeeded();
            await _approvalTaskRepository.UpdateAsync(task, cancellationToken);
            foreach (var domainEvent in task.DomainEvents)
            {
                if (domainEvent is not ApprovalExpiredDomainEvent expired)
                    continue;

                await _historyRepository.AddAsync(new WorkflowHistory(
                    expired.WorkflowInstanceId,
                    expired.OrganizationId,
                    HistoryEventType.ApprovalExpired,
                    "Approval expired",
                    expired.StateId,
                    expired.ApprovalTaskId), cancellationToken);

                var envelope = new EventEnvelope
                {
                    EventId = Guid.NewGuid(),
                    EventType = nameof(ApprovalExpiredDomainEvent),
                    OccurredAt = expired.ExpiredAt,
                    OrganizationId = expired.OrganizationId,
                    Data = expired
                };
                await _outboxRepository.AddAsync(
                    OutboxMessage.Create(envelope.EventType, nameof(ApprovalTask), expired.ApprovalTaskId, envelope),
                    cancellationToken);
            }
            task.ClearDomainEvents();
        }

        if (tasks.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tasks.Count;
    }
}
