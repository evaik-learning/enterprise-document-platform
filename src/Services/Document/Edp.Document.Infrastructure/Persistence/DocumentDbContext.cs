using Microsoft.EntityFrameworkCore;
using DocumentEntity = global::Edp.Document.Domain.Entities.Document;
using DocumentVersionEntity = global::Edp.Document.Domain.Entities.DocumentVersion;
using DocumentFileEntity = global::Edp.Document.Domain.Entities.DocumentFile;
using DocumentGenerationJobEntity = global::Edp.Document.Domain.Entities.DocumentGenerationJob;

namespace Edp.Document.Infrastructure.Persistence;

public sealed class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentEntity> Documents { get; set; } = null!;
    public DbSet<DocumentVersionEntity> DocumentVersions { get; set; } = null!;
    public DbSet<DocumentFileEntity> DocumentFiles { get; set; } = null!;
    public DbSet<DocumentGenerationJobEntity> DocumentGenerationJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DocumentEntity>(entity =>
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
        });

        modelBuilder.Entity<DocumentVersionEntity>(entity =>
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

            entity.HasOne<DocumentEntity>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentFileEntity>(entity =>
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

            entity.HasOne<DocumentEntity>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne<DocumentVersionEntity>()
                .WithMany(x => x.Files)
                .HasForeignKey(x => x.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentGenerationJobEntity>(entity =>
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

            entity.HasOne<DocumentEntity>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
