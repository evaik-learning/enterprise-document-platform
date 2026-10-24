using Edp.Notification.Domain.Entities;
using Xunit;
using NotificationEntity = Edp.Notification.Domain.Entities.Notification;

namespace Edp.Notification.Tests;

public sealed class NotificationPreferenceTests
{
    private static readonly Guid Organization = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();

    [Fact]
    public void DefaultsMatchProductRequirements()
    {
        var preference = NotificationPreference.CreateDefault(Organization, User);

        Assert.True(preference.EmailEnabled);
        Assert.True(preference.InAppEnabled);
        Assert.True(preference.WorkflowEnabled);
        Assert.True(preference.SignatureEnabled);
        Assert.False(preference.AuditEnabled);
    }

    [Theory]
    [InlineData("ApprovalAssigned", false, true, false)]
    [InlineData("WorkflowCompleted", false, true, false)]
    [InlineData("SignatureRequested", true, false, false)]
    [InlineData("DocumentGenerated", true, true, true)]
    public void CategoryTogglesControlInAppDelivery(string type, bool workflow, bool signature, bool expectedWhenCategoryOff)
    {
        var preference = NotificationPreference.CreateDefault(Organization, User);
        preference.Update(emailEnabled: true, inAppEnabled: true, workflowEnabled: workflow, signatureEnabled: signature, auditEnabled: false);

        Assert.Equal(expectedWhenCategoryOff, preference.AllowsInApp(type));
    }

    [Fact]
    public void DisablingInAppBlocksEveryType()
    {
        var preference = NotificationPreference.CreateDefault(Organization, User);
        preference.Update(true, inAppEnabled: false, true, true, false);

        Assert.False(preference.AllowsInApp("DocumentGenerated"));
    }

    [Fact]
    public void NotificationKeepsResourceReferenceOnlyWhenComplete()
    {
        var resourceId = Guid.NewGuid();

        var linked = NotificationEntity.Create(Organization, User, "ApprovalAssigned", "s", "b", Guid.NewGuid(), "c", resourceType: "Workflow", resourceId: resourceId);
        var unlinked = NotificationEntity.Create(Organization, User, "ApprovalAssigned", "s", "b", Guid.NewGuid(), "c", resourceType: "Workflow", resourceId: Guid.Empty);

        Assert.Equal("Workflow", linked.ResourceType);
        Assert.Equal(resourceId, linked.ResourceId);
        Assert.Null(unlinked.ResourceType);
        Assert.Null(unlinked.ResourceId);
    }
}
