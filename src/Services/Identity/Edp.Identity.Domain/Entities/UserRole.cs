using Edp.SharedKernel.Entities;

namespace Edp.Identity.Domain.Entities;

public sealed class UserRole : AuditableEntity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public static UserRole Create(Guid id, Guid userId, Guid roleId)
    {
        return new UserRole
        {
            Id = id,
            UserId = userId,
            RoleId = roleId
        };
    }
}
