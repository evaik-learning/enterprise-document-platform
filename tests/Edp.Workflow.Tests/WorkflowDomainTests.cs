using Edp.Workflow.Domain;
using Edp.Workflow.Application.Services;
using Edp.Workflow.Application.Contracts;
using Edp.Workflow.Contracts.Events;
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

    [Fact]
    public void IntegrationEventMapperUsesVersionedWorkflowStartedContract()
    {
        var organizationId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var instance = new WorkflowInstance(organizationId, Guid.NewGuid(), workflowId, 3, Guid.NewGuid(), Guid.NewGuid());
        var domainEvent = new WorkflowStartedDomainEvent(
            instance.Id, workflowId, instance.DocumentId, instance.InitiatedBy, organizationId);

        var mapped = WorkflowIntegrationEventMapper.Map(domainEvent, instance, 3, "correlation-id");

        Assert.NotNull(mapped);
        Assert.Equal(nameof(WorkflowStartedEvent), mapped.Value.EventType);
        var payload = Assert.IsType<WorkflowStartedEvent>(mapped.Value.Payload);
        Assert.Equal(3, payload.WorkflowVersion);
        Assert.Equal(instance.DocumentId, payload.DocumentId);
    }

    [Fact]
    public void IntegrationEventMapperDoesNotPublishUnsupportedDomainEvents()
    {
        var instance = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid());
        var mapped = WorkflowIntegrationEventMapper.Map(
            new WorkflowPausedDomainEvent(instance.Id, Guid.NewGuid(), "pause", instance.OrganizationId),
            instance,
            1,
            "correlation-id");

        Assert.Null(mapped);
    }

    [Fact]
    public void DocumentGeneratedEventLeavesWorkflowSelectionOptional()
    {
        var documentEvent = new DocumentGeneratedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "correlation-id");

        Assert.Null(documentEvent.WorkflowId);
    }

    [Fact]
    public void OutboxMessageCanBeMarkedDeadLetter()
    {
        var message = OutboxMessage.Create(
            nameof(WorkflowStartedEvent), "WorkflowInstance", Guid.NewGuid(), new { Value = "payload" });

        message.MarkFailed("publish failed");
        message.MarkDeadLetter("retry limit reached");

        Assert.Equal(OutboxStatus.DeadLetter, message.Status);
        Assert.Equal("retry limit reached", message.Error);
        Assert.Equal(1, message.RetryCount);
    }
}
