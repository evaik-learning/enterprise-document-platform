using Edp.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edp.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Email).IsRequired().HasMaxLength(256);
        entity.Property(x => x.NormalizedEmail).IsRequired().HasMaxLength(256);
        entity.Property(x => x.DisplayName).HasMaxLength(256);
        entity.HasIndex(x => x.Email).IsUnique();
        entity.HasIndex(x => x.NormalizedEmail).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.ToTable("Roles");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired().HasMaxLength(128);
        entity.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
        entity.ToTable("UserRoles");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
    }
}
