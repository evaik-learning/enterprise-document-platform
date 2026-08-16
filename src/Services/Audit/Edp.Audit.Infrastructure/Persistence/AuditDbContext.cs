using Edp.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edp.Audit.Infrastructure.Persistence;

public sealed class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).IsRequired().HasMaxLength(200);
            entity.Property(x => x.EntityType).IsRequired().HasMaxLength(200);
            entity.Property(x => x.CorrelationId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.IpAddress).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Metadata).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Timestamp);
        });
    }
}
