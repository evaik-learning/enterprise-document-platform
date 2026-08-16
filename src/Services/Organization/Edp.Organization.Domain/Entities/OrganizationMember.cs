using Edp.SharedKernel.Entities;

namespace Edp.Organization.Domain.Entities;

public sealed class OrganizationMember : AuditableEntity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = "Member";
    public bool IsActive { get; private set; } = true;

    public static OrganizationMember Create(Guid id, Guid organizationId, Guid userId, string role = "Member")
    {
        return new OrganizationMember
        {
            Id = id,
            OrganizationId = organizationId,
            UserId = userId,
            Role = string.IsNullOrWhiteSpace(role) ? "Member" : role.Trim()
        };
    }

    public void UpdateRole(string role)
    {
        Role = string.IsNullOrWhiteSpace(role) ? "Member" : role.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
