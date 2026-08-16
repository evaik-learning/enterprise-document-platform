using Xunit;

namespace Edp.Organization.Tests;

public class OrganizationDomainTests
{
    [Fact]
    public void Organization_Create_SetsSlugAndState()
    {
        var id = Guid.NewGuid();

        var organization = global::Edp.Organization.Domain.Entities.Organization.Create(id, "Contoso Corp", "Example org");

        Assert.Equal(id, organization.Id);
        Assert.Equal("Contoso Corp", organization.Name);
        Assert.Equal("contoso-corp", organization.Slug);
        Assert.Equal("Example org", organization.Description);
        Assert.True(organization.IsActive);
    }

    [Fact]
    public void OrganizationMember_Create_UsesDefaultRole()
    {
        var member = global::Edp.Organization.Domain.Entities.OrganizationMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("Member", member.Role);
        Assert.True(member.IsActive);
    }
}
