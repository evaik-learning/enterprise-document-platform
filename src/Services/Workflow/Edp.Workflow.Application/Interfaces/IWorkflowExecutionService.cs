using Edp.Workflow.Domain;

namespace Edp.Workflow.Application.Interfaces;

public interface IWorkflowExecutionService
{
    Task<WorkflowInstance> StartAsync(
        Guid organizationId,
        Guid workflowId,
        Guid documentId,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ApprovalTask> ApproveAsync(
        Guid organizationId,
        Guid taskId,
        Guid actorUserId,
        string? comment = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalTask> RejectAsync(
        Guid organizationId,
        Guid taskId,
        Guid actorUserId,
        string reason,
        CancellationToken cancellationToken = default);
    Task<ApprovalTask> DelegateAsync(Guid organizationId, Guid taskId, Guid delegateToUserId, Guid actorUserId, string reason, CancellationToken cancellationToken = default);

    Task<WorkflowInstance> CancelAsync(Guid organizationId, Guid instanceId, Guid actorUserId, string reason, CancellationToken cancellationToken = default);
    Task<WorkflowInstance> SuspendAsync(Guid organizationId, Guid instanceId, Guid actorUserId, string reason, CancellationToken cancellationToken = default);
    Task<WorkflowInstance> ResumeAsync(Guid organizationId, Guid instanceId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<WorkflowInstance> ExecuteTransitionAsync(Guid organizationId, Guid instanceId, Guid transitionId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task<WorkflowInstance> CompleteSigningAsync(Guid organizationId, Guid instanceId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
}
