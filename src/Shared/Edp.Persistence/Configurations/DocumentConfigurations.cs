using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Edp.Document.Domain.Entities;
using DocumentEntity = Edp.Document.Domain.Entities.Document;

namespace Edp.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> entity)
    {
        entity.ToTable("Documents");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.OrganizationId).IsRequired();
        entity.Property(x => x.DocumentType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.Name).IsRequired().HasMaxLength(250);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.TemplateId).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.CurrentTemplateVersionId);
        entity.Property(x => x.ExternalReference).HasMaxLength(500);
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.CreatedBy).HasMaxLength(200);
        entity.Property(x => x.ModifiedBy).HasMaxLength(200);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.OrganizationId).HasDatabaseName("IX_Documents_OrganizationId");
        entity.HasIndex(x => new { x.OrganizationId, x.Name }).HasDatabaseName("IX_Documents_OrganizationId_Name");
    }
}

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> entity)
    {
        entity.ToTable("DocumentVersions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.DocumentId).IsRequired();
        entity.Property(x => x.VersionNumber).IsRequired();
        entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
        entity.Property(x => x.TemplateId).IsRequired();
        entity.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        entity.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
        entity.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.DocumentId).HasDatabaseName("IX_DocumentVersions_DocumentId");
        entity.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique().HasDatabaseName("UX_DocumentVersions_DocumentId_VersionNumber");
        entity.HasOne<DocumentEntity>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> entity)
    {
        entity.ToTable("DocumentFiles");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.DocumentId).IsRequired();
        entity.Property(x => x.DocumentVersionId).IsRequired();
        entity.Property(x => x.FileType).IsRequired().HasMaxLength(50);
        entity.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        entity.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
        entity.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.DocumentVersionId).HasDatabaseName("IX_DocumentFiles_DocumentVersionId");
        entity.HasOne<DocumentEntity>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne<DocumentVersion>().WithMany(x => x.Files).HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DocumentGenerationJobConfiguration : IEntityTypeConfiguration<DocumentGenerationJob>
{
    public void Configure(EntityTypeBuilder<DocumentGenerationJob> entity)
    {
        entity.ToTable("DocumentGenerationJobs");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.DocumentId).IsRequired();
        entity.Property(x => x.OrganizationId).IsRequired();
        entity.Property(x => x.TemplateId).IsRequired();
        entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
        entity.Property(x => x.Error).HasMaxLength(2000);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.DocumentId).HasDatabaseName("IX_DocumentGenerationJobs_DocumentId");
        entity.HasOne<DocumentEntity>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}
