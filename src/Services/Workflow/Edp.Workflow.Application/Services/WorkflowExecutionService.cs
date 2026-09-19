using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Application.Contracts;
using Edp.Workflow.Domain;
using Edp.Shared.Contracts;
using Edp.SharedKernel.Domain;
using Edp.Shared.Infrastructure.Persistence;

namespace Edp.Workflow.Application.Services;

public sealed class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowVersionRepository _versionRepository;
    private readonly IWorkflowInstanceRepository _instanceRepository;
    private readonly IApprovalTaskRepository _approvalTaskRepository;
    private readonly IWorkflowHistoryRepository _historyRepository;
    private readonly IWorkflowStateMachine _stateMachine;
    private readonly IWorkflowTransitionRepository _transitionRepository;
    private readonly IAssignmentResolver _assignmentResolver;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkflowExecutionService(
        IWorkflowRepository workflowRepository,
        IWorkflowVersionRepository versionRepository,
        IWorkflowInstanceRepository instanceRepository,
        IApprovalTaskRepository approvalTaskRepository,
        IUnitOfWork unitOfWork,
        IWorkflowStateMachine stateMachine,
        IOutboxMessageRepository outboxRepository,
        IWorkflowHistoryRepository historyRepository,
        IWorkflowTransitionRepository transitionRepository,
        IAssignmentResolver assignmentResolver)
    {
        _workflowRepository = workflowRepository;
        _versionRepository = versionRepository;
        _instanceRepository = instanceRepository;
        _approvalTaskRepository = approvalTaskRepository;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _outboxRepository = outboxRepository;
        _historyRepository = historyRepository;
        _transitionRepository = transitionRepository;
        _assignmentResolver = assignmentResolver;
    }

    public async Task<WorkflowInstance> StartAsync(
        Guid organizationId,
        Guid workflowId,
        Guid documentId,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRepository.GetByIdAsync(organizationId, workflowId, cancellationToken)
            ?? throw new WorkflowNotFoundException(workflowId);

        if (workflow.Status != WorkflowStatus.Published || !workflow.PublishedVersion.HasValue)
            throw new InvalidOperationException("Only a published workflow can be started");

        var version = await _versionRepository.GetForExecutionAsync(
            organizationId, workflowId, workflow.PublishedVersion.Value, cancellationToken)
            ?? throw new InvalidWorkflowVersionException(workflow.PublishedVersion.Value, workflow.LatestVersion);

        var instance = new WorkflowInstance(
            organizationId, documentId, workflowId, version.Version, actorUserId, version.Id);
        instance.Start(version.StartStateId, actorUserId);
        await _instanceRepository.AddAsync(instance, cancellationToken);
        await AdvanceAsync(instance, version, actorUserId, correlationId, cancellationToken);
        await EnqueueDomainEventsAsync(instance, correlationId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return instance;
    }

    public async Task<ApprovalTask> ApproveAsync(
        Guid organizationId,
        Guid taskId,
        Guid actorUserId,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var task = await _approvalTaskRepository.GetByIdAsync(organizationId, taskId, cancellationToken)
            ?? throw new ApprovalTaskNotFoundException(taskId);
        var instance = await _instanceRepository.GetByIdAsync(organizationId, task.WorkflowInstanceId, cancellationToken)
            ?? throw new WorkflowInstanceNotFoundException(task.WorkflowInstanceId);
        if (instance.Status == InstanceStatus.Paused)
            throw new InvalidWorkflowStateException(instance.Id, instance.Status, "approve");
        if (instance.CurrentStateId != task.StateId)
            throw new InvalidOperationException("Approval task does not belong to the current workflow state.");
        task.Approve(actorUserId, comment);
        instance.RecordApprovalApproved(task.Id, task.StateId, actorUserId, comment);
        await _approvalTaskRepository.UpdateAsync(task, cancellationToken);
        var version = await _versionRepository.GetForExecutionAsync(
            organizationId, instance.WorkflowId, instance.WorkflowVersion, cancellationToken)
            ?? throw new InvalidWorkflowVersionException(instance.WorkflowVersion, instance.WorkflowVersion);
        var state = version.GetState(task.StateId)
            ?? throw new WorkflowStateNotFoundException(task.StateId);
        var tasks = await _approvalTaskRepository.ListByInstanceAsync(organizationId, instance.Id, task.StateId, cancellationToken);
        var policy = state.GetApprovalPolicy()?.Type ?? GetPolicyFromState(state.StateType);
        if (policy == ApprovalPolicyType.ParallelAny)
        {
            foreach (var sibling in tasks.Where(candidate => candidate.Id != task.Id && candidate.Status == ApprovalStatus.Pending))
                sibling.Cancel();
            await AdvanceAsync(instance, version, actorUserId, instance.Id.ToString(), cancellationToken);
        }
        else if (tasks.All(candidate => candidate.Status == ApprovalStatus.Approved))
        {
            await AdvanceAsync(instance, version, actorUserId, instance.Id.ToString(), cancellationToken);
        }
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, instance.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<WorkflowInstance> CancelAsync(Guid organizationId, Guid instanceId, Guid actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        var instance = await GetInstanceAsync(organizationId, instanceId, cancellationToken);
        instance.Cancel(actorUserId, reason);
        await CancelPendingTasksAsync(organizationId, instance.Id, cancellationToken);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, instance.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return instance;
    }

    public async Task<ApprovalTask> DelegateAsync(Guid organizationId, Guid taskId, Guid delegateToUserId, Guid actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        if (delegateToUserId == Guid.Empty)
            throw new ArgumentException("Delegate user is required.", nameof(delegateToUserId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Delegation reason is required.", nameof(reason));

        var task = await _approvalTaskRepository.GetByIdAsync(organizationId, taskId, cancellationToken)
            ?? throw new ApprovalTaskNotFoundException(taskId);
        var fromUserId = task.AssignedToUserId;
        var instance = await GetInstanceAsync(organizationId, task.WorkflowInstanceId, cancellationToken);
        if (instance.Status == InstanceStatus.Paused || instance.CurrentStateId != task.StateId)
            throw new InvalidWorkflowStateException(instance.Id, instance.Status, "delegate");
        task.Reassign(delegateToUserId, actorUserId, reason);
        await _approvalTaskRepository.UpdateAsync(task, cancellationToken);
        instance.RecordApprovalReassigned(task.Id, fromUserId, delegateToUserId, reason);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, instance.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<WorkflowInstance> SuspendAsync(Guid organizationId, Guid instanceId, Guid actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        var instance = await GetInstanceAsync(organizationId, instanceId, cancellationToken);
        instance.Pause(actorUserId, reason);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, instance.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return instance;
    }

    public async Task<WorkflowInstance> ResumeAsync(Guid organizationId, Guid instanceId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var instance = await GetInstanceAsync(organizationId, instanceId, cancellationToken);
        instance.Resume(actorUserId);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, instance.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return instance;
    }

    public async Task<WorkflowInstance> ExecuteTransitionAsync(
        Guid organizationId,
        Guid instanceId,
        Guid transitionId,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var instance = await GetInstanceAsync(organizationId, instanceId, cancellationToken);
        if (instance.Status != InstanceStatus.InProgress)
            throw new InvalidWorkflowStateException(instance.Id, instance.Status, "execute transition");

        var version = await _versionRepository.GetForExecutionAsync(
            organizationId, instance.WorkflowId, instance.WorkflowVersion, cancellationToken)
            ?? throw new InvalidWorkflowVersionException(instance.WorkflowVersion, instance.WorkflowVersion);
        var transition = (await _transitionRepository.ListAsync(organizationId, version.Id, cancellationToken))
            .FirstOrDefault(candidate => candidate.Id == transitionId)
            ?? throw new InvalidOperationException($"Transition '{transitionId}' was not found.");
        var variables = instance.Variables.ToDictionary(variable => variable.Name, variable => (object?)variable.Value, StringComparer.OrdinalIgnoreCase);
        var context = new WorkflowExecutionContext(
            instance.Id, instance.DocumentId, instance.OrganizationId, actorUserId, [], variables, correlationId);
        var result = _stateMachine.Execute(instance, version, transition, context);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        await AdvanceAsync(instance, version, actorUserId, correlationId, cancellationToken);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, correlationId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return instance;
    }

    public async Task<WorkflowInstance> CompleteSigningAsync(
        Guid organizationId,
        Guid instanceId,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var instance = await GetInstanceAsync(organizationId, instanceId, cancellationToken);
        if (instance.Status != InstanceStatus.InProgress || !instance.CurrentStateId.HasValue)
            throw new InvalidWorkflowStateException(instance.Id, instance.Status, "complete signing");

        var version = await _versionRepository.GetForExecutionAsync(
            organizationId, instance.WorkflowId, instance.WorkflowVersion, cancellationToken)
            ?? throw new InvalidWorkflowVersionException(instance.WorkflowVersion, instance.WorkflowVersion);
        var transition = (await _transitionRepository.ListAsync(organizationId, version.Id, cancellationToken))
            .Where(candidate => candidate.FromStateId == instance.CurrentStateId.Value)
            .FirstOrDefault(candidate => string.Equals(candidate.TriggerType, "signing-completed", StringComparison.OrdinalIgnoreCase));
        if (transition is null)
            throw new InvalidOperationException("The current workflow state has no signing-completed transition.");

        var context = new WorkflowExecutionContext(
            instance.Id,
            instance.DocumentId,
            instance.OrganizationId,
            actorUserId,
            [],
            instance.Variables.ToDictionary(variable => variable.Name, variable => (object?)variable.Value, StringComparer.OrdinalIgnoreCase),
            correlationId);
        var result = _stateMachine.Execute(instance, version, transition, context);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        await AdvanceAsync(instance, version, actorUserId, correlationId, cancellationToken);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, correlationId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return instance;
    }

    public async Task<ApprovalTask> RejectAsync(
        Guid organizationId,
        Guid taskId,
        Guid actorUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var task = await _approvalTaskRepository.GetByIdAsync(organizationId, taskId, cancellationToken)
            ?? throw new ApprovalTaskNotFoundException(taskId);
        var instance = await _instanceRepository.GetByIdAsync(organizationId, task.WorkflowInstanceId, cancellationToken)
            ?? throw new WorkflowInstanceNotFoundException(task.WorkflowInstanceId);
        if (instance.Status == InstanceStatus.Paused || instance.CurrentStateId != task.StateId)
            throw new InvalidWorkflowStateException(instance.Id, instance.Status, "reject");
        task.Reject(actorUserId, reason);
        await _approvalTaskRepository.UpdateAsync(task, cancellationToken);
        var version = await _versionRepository.GetForExecutionAsync(
            organizationId, instance.WorkflowId, instance.WorkflowVersion, cancellationToken)
            ?? throw new InvalidWorkflowVersionException(instance.WorkflowVersion, instance.WorkflowVersion);
        var rejectionTransition = (await _transitionRepository.ListAsync(organizationId, version.Id, cancellationToken))
            .Where(transition => transition.FromStateId == task.StateId)
            .FirstOrDefault(transition => string.Equals(transition.TriggerType, "reject", StringComparison.OrdinalIgnoreCase));
        if (rejectionTransition is null)
            throw new InvalidOperationException("The approval state has no configured reject transition.");

        var context = new WorkflowExecutionContext(
            instance.Id,
            instance.DocumentId,
            instance.OrganizationId,
            actorUserId,
            [],
            instance.Variables.ToDictionary(variable => variable.Name, variable => (object?)variable.Value, StringComparer.OrdinalIgnoreCase),
            instance.Id.ToString());
        var result = _stateMachine.Execute(instance, version, rejectionTransition, context);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        instance.RecordApprovalRejected(task.Id, task.StateId, actorUserId, reason);
        await CancelPendingTasksAsync(organizationId, instance.Id, cancellationToken);
        await AdvanceAsync(instance, version, actorUserId, instance.Id.ToString(), cancellationToken);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);
        await EnqueueDomainEventsAsync(instance, instance.Id.ToString(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task;
    }

    private async Task AdvanceAsync(
        WorkflowInstance instance,
        WorkflowVersion version,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        while (instance.CurrentStateId.HasValue)
        {
            var state = version.GetState(instance.CurrentStateId.Value)
                ?? throw new WorkflowStateNotFoundException(instance.CurrentStateId.Value);
            if (state.StateType == StateType.End)
            {
                instance.Complete(actorUserId);
                return;
            }

            if (state.StateType is StateType.Approval or StateType.ParallelApprovalAll or StateType.ParallelApprovalAny)
            {
                var policy = state.GetApprovalPolicy();
                var approverUserIds = policy?.ApproverUserIds
                    ?? await _assignmentResolver.ResolveAsync(state.AssignmentRules, actorUserId, [], cancellationToken);
                if (approverUserIds.Count == 0)
                    throw new InvalidOperationException($"Approval state '{state.Name}' has no eligible approvers.");
                foreach (var userId in approverUserIds)
                {
                    var task = instance.CreateApprovalTask(state.Id, userId);
                    await _approvalTaskRepository.AddAsync(task, cancellationToken);
                }
                return;
            }

            var transition = version.GetTransitionsFrom(state.Id).FirstOrDefault();
            if (transition is null)
                throw new InvalidOperationException($"State '{state.Name}' has no executable transition.");
            var context = new WorkflowExecutionContext(
                instance.Id, instance.DocumentId, instance.OrganizationId, actorUserId, [], new Dictionary<string, object?>(), correlationId);
            var result = _stateMachine.Execute(instance, version, transition, context);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);
        }
    }

    private static ApprovalPolicyType GetPolicyFromState(StateType stateType) => stateType switch
    {
        StateType.ParallelApprovalAll => ApprovalPolicyType.ParallelAll,
        StateType.ParallelApprovalAny => ApprovalPolicyType.ParallelAny,
        _ => ApprovalPolicyType.Sequential
    };

    private async Task<WorkflowInstance> GetInstanceAsync(Guid organizationId, Guid instanceId, CancellationToken cancellationToken) =>
        await _instanceRepository.GetByIdAsync(organizationId, instanceId, cancellationToken)
        ?? throw new WorkflowInstanceNotFoundException(instanceId);

    private async Task CancelPendingTasksAsync(Guid organizationId, Guid instanceId, CancellationToken cancellationToken)
    {
        var tasks = await _approvalTaskRepository.ListByInstanceAsync(organizationId, instanceId, cancellationToken: cancellationToken);
        foreach (var task in tasks.Where(task => task.Status == ApprovalStatus.Pending))
        {
            task.Cancel();
            await _approvalTaskRepository.UpdateAsync(task, cancellationToken);
        }
    }

    private async Task EnqueueDomainEventsAsync(
        WorkflowInstance instance,
        string correlationId,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in instance.DomainEvents)
        {
            await _historyRepository.AddAsync(CreateHistory(instance, domainEvent), cancellationToken);
            var integrationEvent = WorkflowIntegrationEventMapper.Map(
                domainEvent,
                instance,
                instance.WorkflowVersion,
                correlationId);
            if (integrationEvent is null)
                continue;

            var eventId = integrationEvent.Value.Payload.GetType().GetProperty("EventId")?.GetValue(integrationEvent.Value.Payload) as Guid?
                ?? Guid.NewGuid();
            var envelope = new EventEnvelope
            {
                EventId = eventId,
                EventType = integrationEvent.Value.EventType,
                OccurredAt = DateTimeOffset.UtcNow,
                OrganizationId = instance.OrganizationId,
                UserId = GetEventUserId(domainEvent),
                CorrelationId = Guid.TryParse(correlationId, out var parsedCorrelationId) ? parsedCorrelationId : null,
                Data = integrationEvent.Value.Payload
            };
            await _outboxRepository.AddAsync(
                OutboxMessage.Create(envelope.EventType, nameof(WorkflowInstance), instance.Id, envelope),
                cancellationToken);
        }
        instance.ClearDomainEvents();
    }

    private static WorkflowHistory CreateHistory(WorkflowInstance instance, DomainEvent domainEvent)
    {
        var (eventType, description, stateId, taskId) = domainEvent switch
        {
            WorkflowStartedDomainEvent => (HistoryEventType.InstanceStarted, "Workflow started", (Guid?)null, (Guid?)null),
            WorkflowTransitionedDomainEvent transitioned => (HistoryEventType.StateEntered, "Workflow state changed", transitioned.ToStateId, (Guid?)null),
            ApprovalAssignedDomainEvent assigned => (HistoryEventType.ApprovalAssigned, "Approval assigned", assigned.StateId, assigned.ApprovalTaskId),
            ApprovalApprovedDomainEvent approved => (HistoryEventType.ApprovalApproved, "Approval approved", approved.StateId, approved.ApprovalTaskId),
            ApprovalRejectedDomainEvent rejected => (HistoryEventType.ApprovalRejected, "Approval rejected", rejected.StateId, rejected.ApprovalTaskId),
            WorkflowCompletedDomainEvent => (HistoryEventType.InstanceCompleted, "Workflow completed", (Guid?)null, (Guid?)null),
            WorkflowRejectedDomainEvent => (HistoryEventType.InstanceRejected, "Workflow rejected", (Guid?)null, (Guid?)null),
            WorkflowCancelledDomainEvent => (HistoryEventType.InstanceCancelled, "Workflow cancelled", (Guid?)null, (Guid?)null),
            _ => (HistoryEventType.TransitionEvaluated, domainEvent.GetType().Name, (Guid?)null, (Guid?)null)
        };

        return new WorkflowHistory(instance.Id, instance.OrganizationId, eventType, description, stateId, taskId, GetEventUserId(domainEvent));
    }

    private static Guid? GetEventUserId(DomainEvent domainEvent) => domainEvent switch
    {
        WorkflowStartedDomainEvent started => started.InitiatedBy,
        WorkflowCompletedDomainEvent completed => completed.CompletedBy,
        WorkflowRejectedDomainEvent rejected => rejected.RejectedBy,
        WorkflowCancelledDomainEvent cancelled => cancelled.CancelledBy,
        WorkflowPausedDomainEvent paused => paused.PausedBy,
        WorkflowResumedDomainEvent resumed => resumed.ResumedBy,
        ApprovalApprovedDomainEvent approved => approved.ApprovedByUserId,
        ApprovalRejectedDomainEvent rejected => rejected.RejectedByUserId,
        _ => null
    };
}
