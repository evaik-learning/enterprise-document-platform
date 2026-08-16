using Xunit;

namespace Edp.Audit.Tests;

public class AuditDomainTests
{
    [Fact]
    public void AuditLog_Create_SetsRequiredProperties()
    {
        var id = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var metadata = new Dictionary<string, object?>
        {
            ["email"] = "admin@example.com",
            ["role"] = "Owner"
        };

        var auditLog = global::Edp.Audit.Domain.Entities.AuditLog.Create(
            id,
            organizationId,
            userId,
            "UserRegistered",
            "User",
            id,
            "correlation-123",
            "127.0.0.1",
            metadata);

        Assert.Equal(id, auditLog.Id);
        Assert.Equal(organizationId, auditLog.OrganizationId);
        Assert.Equal(userId, auditLog.UserId);
        Assert.Equal("UserRegistered", auditLog.Action);
        Assert.Equal("User", auditLog.EntityType);
        Assert.Equal(id, auditLog.EntityId);
        Assert.Equal("correlation-123", auditLog.CorrelationId);
        Assert.Equal("127.0.0.1", auditLog.IpAddress);
        Assert.Equal("admin@example.com", auditLog.Metadata?["email"]);
        Assert.NotEqual(default, auditLog.Timestamp);
        Assert.True(auditLog.Timestamp <= DateTimeOffset.UtcNow);
    }
}
