namespace Edp.Workflow.Application;

public sealed class WorkflowOptions
{
    public int MaxHistoryPageSize { get; set; } = 100;
    public int DefaultApprovalTimeoutHours { get; set; } = 48;
    public int OutboxBatchSize { get; set; } = 100;
    public int TimeoutWorkerIntervalSeconds { get; set; } = 30;
    public int OutboxPollIntervalSeconds { get; set; } = 10;
    public int MaxOutboxRetryAttempts { get; set; } = 10;
}
