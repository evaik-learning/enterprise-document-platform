using Edp.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Edp.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("AuditLogs");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Action).IsRequired().HasMaxLength(200);
        entity.Property(x => x.EntityType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.CorrelationId).IsRequired().HasMaxLength(200);
        entity.Property(x => x.IpAddress).IsRequired().HasMaxLength(64);
        entity.Property(x => x.Metadata)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<Dictionary<string, object>>(value, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("nvarchar(max)");
        entity.HasIndex(x => x.OrganizationId);
        entity.HasIndex(x => x.UserId);
        entity.HasIndex(x => x.Timestamp);
    }
}
