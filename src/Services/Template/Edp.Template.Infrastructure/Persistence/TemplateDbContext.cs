using Microsoft.EntityFrameworkCore;
using Edp.Template.Domain.Entities;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.Template>(b =>
        {
            b.ToTable("Templates");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(250);
            b.Property(x => x.Status).HasMaxLength(50);
            b.Property(x => x.CurrentVersionId);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.TemplateVersion>(b =>
        {
            b.ToTable("TemplateVersions");
            b.HasKey(x => x.Id);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(500);
            b.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.Placeholder>(b =>
        {
            b.ToTable("Placeholders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(250);
            b.Property(x => x.DataType).HasMaxLength(50);
        });

        modelBuilder.Entity<global::Edp.Template.Domain.Entities.ValidationResultEntity>(b =>
        {
            b.ToTable("ValidationResults");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasMaxLength(50);
            b.Property(x => x.ValidatedAt);
        });
    }
}
