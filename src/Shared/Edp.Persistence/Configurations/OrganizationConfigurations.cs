using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Edp.Organization.Domain.Entities;
using OrganizationEntity = Edp.Organization.Domain.Entities.Organization;

namespace Edp.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<OrganizationEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationEntity> entity)
    {
        entity.ToTable("Organizations");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        entity.Property(x => x.Slug).IsRequired().HasMaxLength(200);
        entity.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> entity)
    {
        entity.ToTable("OrganizationMembers");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Role).IsRequired().HasMaxLength(100);
        entity.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
    }
}
