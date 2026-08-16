using Microsoft.EntityFrameworkCore;
using Edp.Template.Domain.Entities;
using Edp.Template.Domain.Enums;

namespace Edp.Template.Infrastructure.Persistence;

public sealed class TemplateDbContext : DbContext
{
    public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
    {
    }

    public DbSet<global::Edp.Template.Domain.Entities.Template> Templates { get; set; } = null!;
    public DbSet<global::Edp.Template.Domain.Entities.TemplateVersion> TemplateVersions { get; set; } = null!;
    public DbSet<global::Edp.Template.Domain.Entities.Placeholder> Placeholders { get; set; } = null!;
    public DbSet<global::Edp.Template.Domain.Entities.ValidationResultEntity> ValidationResults { get; set; } = null!;
    public DbSet<global::Edp.Template.Application.Contracts.OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.Template>(b =>
        {
            b.ToTable("Templates");
            b.HasKey(x => x.Id);
            b.Property(x => x.OrganizationId).IsRequired();
            b.Property(x => x.Name).IsRequired().HasMaxLength(250);
            b.Property(x => x.Code).IsRequired().HasMaxLength(100);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.CreatedBy).HasMaxLength(100);
            b.Property(x => x.ModifiedBy).HasMaxLength(100);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => x.OrganizationId).HasDatabaseName("IX_Templates_OrganizationId");
            b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasDatabaseName("UX_Templates_OrganizationId_Code");
        });

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.TemplateVersion>(b =>
        {
            b.ToTable("TemplateVersions");
            b.HasKey(x => x.Id);
            b.Property(x => x.OrganizationId).IsRequired();
            b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            b.Property(x => x.BlobContainer).IsRequired().HasMaxLength(100);
            b.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
            b.Property(x => x.FileHash).HasMaxLength(128);
            b.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
            b.Property(x => x.ValidationStatus).HasConversion<string>().HasMaxLength(50);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            b.Property(x => x.ChangeDescription).HasMaxLength(1000);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.CreatedBy).HasMaxLength(100);
            b.Property(x => x.ModifiedBy).HasMaxLength(100);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => x.TemplateId).HasDatabaseName("IX_TemplateVersions_TemplateId");
            b.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique().HasDatabaseName("UX_TemplateVersions_TemplateId_VersionNumber");
            b.HasOne<global::Edp.Template.Domain.Entities.Template>()
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.Placeholder>(b =>
        {
            b.ToTable("Placeholders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.DisplayName).HasMaxLength(200);
            b.Property(x => x.DataType).HasConversion<string>().HasMaxLength(50);
            b.Property(x => x.Format).HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.HasIndex(x => x.TemplateVersionId).HasDatabaseName("IX_TemplatePlaceholders_TemplateVersionId");
            b.HasIndex(x => new { x.TemplateVersionId, x.Name }).IsUnique().HasDatabaseName("UX_TemplatePlaceholders_TemplateVersionId_Name");
            b.HasOne<global::Edp.Template.Domain.Entities.TemplateVersion>()
                .WithMany()
                .HasForeignKey(x => x.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.ValidationResultEntity>(b =>
        {
            b.ToTable("ValidationResults");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            b.Property(x => x.ValidatedAt);
            b.HasIndex(x => x.TemplateVersionId).HasDatabaseName("IX_ValidationResults_TemplateVersionId");
            b.HasOne<global::Edp.Template.Domain.Entities.TemplateVersion>()
                .WithMany()
                .HasForeignKey(x => x.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<global::Edp.Template.Application.Contracts.OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).IsRequired().HasMaxLength(200);
            b.Property(x => x.AggregateType).IsRequired().HasMaxLength(200);
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.OccurredOnUtc).IsRequired();
            b.Property(x => x.Error).HasMaxLength(2000);
            b.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc }).HasDatabaseName("IX_OutboxMessages_Pending");
        });
    }
}
