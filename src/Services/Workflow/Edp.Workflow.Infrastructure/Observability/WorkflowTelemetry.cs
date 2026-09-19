using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Edp.Workflow.Infrastructure.Observability;

public static class WorkflowTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Edp.Workflow");
    public static readonly Meter Meter = new("Edp.Workflow");
    public static readonly Counter<long> InboxProcessed = Meter.CreateCounter<long>("edp.workflow.inbox.processed");
    public static readonly Counter<long> InboxDuplicates = Meter.CreateCounter<long>("edp.workflow.inbox.duplicates");
    public static readonly Counter<long> OutboxPublished = Meter.CreateCounter<long>("edp.workflow.outbox.published");
    public static readonly Counter<long> OutboxFailed = Meter.CreateCounter<long>("edp.workflow.outbox.failed");
    public static readonly Counter<long> OutboxDeadLettered = Meter.CreateCounter<long>("edp.workflow.outbox.dead_lettered");
}