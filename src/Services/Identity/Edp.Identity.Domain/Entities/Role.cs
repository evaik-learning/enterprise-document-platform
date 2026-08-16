using Edp.SharedKernel.Entities;

namespace Edp.Identity.Domain.Entities;

public sealed class Role : AuditableEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public static Role Create(Guid id, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Role
        {
            Id = id,
            Name = name.Trim(),
            Description = description?.Trim()
        };
    }
}
