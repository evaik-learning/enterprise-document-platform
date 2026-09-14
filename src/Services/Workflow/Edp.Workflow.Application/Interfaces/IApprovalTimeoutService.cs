namespace Edp.Workflow.Application.Interfaces;

public interface IApprovalTimeoutService
{
    Task<int> ProcessExpiredAsync(int batchSize, CancellationToken cancellationToken = default);
}
