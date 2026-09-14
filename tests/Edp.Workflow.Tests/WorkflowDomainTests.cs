using Edp.Workflow.Domain;
using Xunit;

namespace Edp.Workflow.Tests;

public sealed class WorkflowDomainTests
{
    [Fact]
    public void StartPendingInstanceMovesToInProgressAndSetsState()
    {
        var organizationId = Guid.NewGuid();
        var instance = new WorkflowInstance(
            organizationId, Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid());
        var startStateId = Guid.NewGuid();

        instance.Start(startStateId, instance.InitiatedBy);

        Assert.Equal(InstanceStatus.InProgress, instance.Status);
        Assert.Equal(startStateId, instance.CurrentStateId);
        Assert.NotNull(instance.StartedAt);
        Assert.Contains(instance.DomainEvents, eventItem => eventItem is WorkflowStartedDomainEvent);
    }

    [Fact]
    public void StartAlreadyStartedInstanceThrows()
    {
        var instance = new WorkflowInstance(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid());
        instance.Start(Guid.NewGuid(), instance.InitiatedBy);

        Assert.Throws<InvalidWorkflowStateException>(() => instance.Start(Guid.NewGuid(), instance.InitiatedBy));
    }

    [Fact]
    public void ApproveAssignedUserChangesStatusAndRecordsAction()
    {
        var assignedUserId = Guid.NewGuid();
        var task = new ApprovalTask(Guid.NewGuid(), Guid.NewGuid(), assignedUserId, Guid.NewGuid());

        task.Approve(assignedUserId, "Approved");

        Assert.Equal(ApprovalStatus.Approved, task.Status);
        Assert.Single(task.Actions);
        Assert.Equal(ApprovalStatus.Approved, task.Actions[0].ActionType);
    }

    [Fact]
    public void ApproveDifferentUserThrows()
    {
        var task = new ApprovalTask(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ApprovalNotPermittedException>(() => task.Approve(Guid.NewGuid()));
    }

    [Fact]
    public void MarkExpiredIfNeededExpiredPendingTaskChangesStatus()
    {
        var task = new ApprovalTask(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1));

        task.MarkExpiredIfNeeded();

        Assert.Equal(ApprovalStatus.Expired, task.Status);
    }

    [Fact]
    public void MarkExpiredIfNeededRaisesExpirationEvent()
    {
        var task = new ApprovalTask(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1));

        task.MarkExpiredIfNeeded();

        var expirationEvent = Assert.Single(task.DomainEvents.OfType<ApprovalExpiredDomainEvent>());
        Assert.Equal(task.Id, expirationEvent.ApprovalTaskId);
        Assert.Equal(task.WorkflowInstanceId, expirationEvent.WorkflowInstanceId);
    }
}
