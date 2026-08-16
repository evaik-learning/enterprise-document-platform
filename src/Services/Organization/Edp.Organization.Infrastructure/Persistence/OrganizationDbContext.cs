using Edp.Organization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edp.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext : DbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : base(options)
    {
    }

    public DbSet<global::Edp.Organization.Domain.Entities.Organization> Organizations => Set<global::Edp.Organization.Domain.Entities.Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<global::Edp.Organization.Domain.Entities.Organization>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        });
    }
}
