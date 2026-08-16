using Edp.SharedKernel.Entities;

namespace Edp.Organization.Domain.Entities;

public sealed class Organization : AuditableEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Organization Create(Guid id, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmedName = name.Trim();
        var slug = trimmedName.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");

        return new Organization
        {
            Id = id,
            Name = trimmedName,
            Slug = slug,
            Description = description?.Trim()
        };
    }

    public void Update(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Slug = Name.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");
        Description = description?.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
