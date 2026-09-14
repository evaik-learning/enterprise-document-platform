using Edp.Template.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Edp.Template.Domain.Entities;
using TemplateEntity = Edp.Template.Domain.Entities.Template;

namespace Edp.Persistence.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<TemplateEntity>
{
    public void Configure(EntityTypeBuilder<TemplateEntity> entity)
    {
        entity.ToTable("Templates");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.OrganizationId).IsRequired();
        entity.Property(x => x.Name).IsRequired().HasMaxLength(250);
        entity.Property(x => x.Code).IsRequired().HasMaxLength(100);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.CreatedBy).HasMaxLength(100);
        entity.Property(x => x.ModifiedBy).HasMaxLength(100);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.OrganizationId).HasDatabaseName("IX_Templates_OrganizationId");
        entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasDatabaseName("UX_Templates_OrganizationId_Code");
    }
}

public sealed class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> entity)
    {
        entity.ToTable("TemplateVersions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.OrganizationId).IsRequired();
        entity.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        entity.Property(x => x.BlobContainer).IsRequired().HasMaxLength(100);
        entity.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
        entity.Property(x => x.FileHash).HasMaxLength(128);
        entity.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.ValidationStatus).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.ChangeDescription).HasMaxLength(1000);
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.CreatedBy).HasMaxLength(100);
        entity.Property(x => x.ModifiedBy).HasMaxLength(100);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.TemplateId).HasDatabaseName("IX_TemplateVersions_TemplateId");
        entity.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique().HasDatabaseName("UX_TemplateVersions_TemplateId_VersionNumber");
        entity.HasOne<TemplateEntity>().WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlaceholderConfiguration : IEntityTypeConfiguration<Placeholder>
{
    public void Configure(EntityTypeBuilder<Placeholder> entity)
    {
        entity.ToTable("Placeholders");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        entity.Property(x => x.DisplayName).HasMaxLength(200);
        entity.Property(x => x.DataType).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.Format).HasMaxLength(200);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.HasIndex(x => x.TemplateVersionId).HasDatabaseName("IX_TemplatePlaceholders_TemplateVersionId");
        entity.HasIndex(x => new { x.TemplateVersionId, x.Name }).IsUnique().HasDatabaseName("UX_TemplatePlaceholders_TemplateVersionId_Name");
        entity.HasOne<TemplateVersion>().WithMany().HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ValidationResultConfiguration : IEntityTypeConfiguration<ValidationResultEntity>
{
    public void Configure(EntityTypeBuilder<ValidationResultEntity> entity)
    {
        entity.ToTable("ValidationResults");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.ValidatedAt);
        entity.HasIndex(x => x.TemplateVersionId).HasDatabaseName("IX_ValidationResults_TemplateVersionId");
        entity.HasOne<TemplateVersion>().WithMany().HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TemplateOutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.ToTable("OutboxMessages", "template");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.EventType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.AggregateType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.Payload).IsRequired();
        entity.Property(x => x.OccurredOnUtc).IsRequired();
        entity.Property(x => x.Error).HasMaxLength(2000);
        entity.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc }).HasDatabaseName("IX_TemplateOutboxMessages_Pending");
    }
}
