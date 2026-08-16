using Edp.Identity.Domain.Entities;
using Xunit;

namespace Edp.Identity.Tests;

public class IdentityDomainTests
{
    [Fact]
    public void User_Create_SetsExpectedValues()
    {
        var id = Guid.NewGuid();

        var user = User.Create(id, " alice@example.com ", "Alice", "Smith");

        Assert.Equal(id, user.Id);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("alice@example.com", user.NormalizedEmail);
        Assert.Equal("Alice", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        Assert.Equal("Alice Smith", user.DisplayName);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_UpdateProfile_UpdatesDisplayName()
    {
        var user = User.Create(Guid.NewGuid(), "user@example.com", "Alice", "Smith");

        user.UpdateProfile("Alicia", "Jones");

        Assert.Equal("Alicia", user.FirstName);
        Assert.Equal("Jones", user.LastName);
        Assert.Equal("Alicia Jones", user.DisplayName);
    }
}
