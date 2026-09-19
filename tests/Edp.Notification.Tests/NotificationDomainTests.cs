using Edp.Notification.Domain.Entities;
using Xunit;
using NotificationEntity = Edp.Notification.Domain.Entities.Notification;

namespace Edp.Notification.Tests;

public sealed class NotificationDomainTests
{
    [Fact]
    public void Create_InAppNotification_SetsPendingReadState()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var notification = NotificationEntity.Create(
            organizationId,
            userId,
            "ApprovalRequired",
            "Approval required",
            "A document requires your approval.",
            eventId,
            "correlation-123");

        Assert.Equal(organizationId, notification.OrganizationId);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Null(notification.ReadAtUtc);
        Assert.Equal(eventId, notification.SourceEventId);
    }

    [Fact]
    public void MarkRead_SetsReadTimestamp()
    {
        var notification = NotificationEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "WorkflowCompleted", "Done", "Completed.", Guid.NewGuid(), "correlation-123");

        notification.MarkRead();

        Assert.Equal(NotificationStatus.Read, notification.Status);
        Assert.NotNull(notification.ReadAtUtc);
    }
}
