using Edp.Notification.Domain.Entities;
using NotificationEntity = Edp.Notification.Domain.Entities.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edp.Persistence.Configurations;

public sealed class NotificationInboxMessageConfiguration : IEntityTypeConfiguration<NotificationInboxMessage>
{
    public void Configure(EntityTypeBuilder<NotificationInboxMessage> entity)
    {
        entity.ToTable("NotificationInboxMessages", "notification");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.MessageId).IsRequired().HasMaxLength(200);
        entity.Property(x => x.EventType).IsRequired().HasMaxLength(250);
        entity.Property(x => x.Payload).IsRequired().HasColumnType("nvarchar(max)");
        entity.Property(x => x.Error).HasMaxLength(4000);
        entity.HasIndex(x => x.MessageId).IsUnique();
        entity.HasIndex(x => new { x.OrganizationId, x.ReceivedAtUtc });
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> entity)
    {
        entity.ToTable("Notifications", "notification");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.NotificationType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.Subject).IsRequired().HasMaxLength(500);
        entity.Property(x => x.Body).IsRequired().HasMaxLength(4000);
        entity.Property(x => x.CorrelationId).IsRequired().HasMaxLength(200);
        entity.HasIndex(x => new { x.OrganizationId, x.UserId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.SourceEventId, x.UserId, x.Channel }).IsUnique();
    }
}
