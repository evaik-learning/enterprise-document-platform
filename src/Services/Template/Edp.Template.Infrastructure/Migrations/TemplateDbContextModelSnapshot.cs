using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.Template.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(Edp.Template.Infrastructure.Persistence.TemplateDbContext))]
    partial class TemplateDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Edp.Template.Domain.Entities.Template", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedNever();
                b.Property<string>("Name").IsRequired().HasMaxLength(250);
                b.Property<string>("Description");
                b.Property<string>("Status").HasMaxLength(50);
                b.Property<Guid?>("CurrentVersionId");
                b.Property<byte[]>("RowVersion").IsRowVersion();
                b.HasKey("Id");
                b.ToTable("Templates");
            });

            modelBuilder.Entity("Edp.Template.Domain.Entities.TemplateVersion", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedNever();
                b.Property<Guid>("TemplateId");
                b.Property<int>("VersionNumber");
                b.Property<string>("FileName").IsRequired().HasMaxLength(500);
                b.Property<string>("StoragePath").IsRequired().HasMaxLength(1000);
                b.Property<string>("FileHash");
                b.Property<long>("FileSize");
                b.Property<string>("ContentType").IsRequired();
                b.Property<string>("ValidationStatus").IsRequired();
                b.Property<string>("Status").IsRequired();
                b.Property<string>("ChangeDescription");
                b.Property<byte[]>("RowVersion").IsRowVersion();
                b.HasKey("Id");
                b.HasIndex("TemplateId");
                b.ToTable("TemplateVersions");
            });

            modelBuilder.Entity("Edp.Template.Domain.Entities.Placeholder", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedNever();
                b.Property<Guid>("TemplateVersionId");
                b.Property<string>("Name").IsRequired().HasMaxLength(250);
                b.Property<string>("DisplayName");
                b.Property<string>("DataType").HasMaxLength(50);
                b.Property<bool>("IsRequired");
                b.Property<string>("DefaultValue");
                b.Property<string>("Format");
                b.Property<string>("Description");
                b.HasKey("Id");
                b.HasIndex("TemplateVersionId");
                b.ToTable("Placeholders");
            });

            modelBuilder.Entity("Edp.Template.Domain.Entities.ValidationResultEntity", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedNever();
                b.Property<Guid>("TemplateVersionId");
                b.Property<string>("Status").HasMaxLength(50);
                b.Property<int>("ErrorCount");
                b.Property<int>("WarningCount");
                b.Property<DateTime>("ValidatedAt");
                b.HasKey("Id");
                b.HasIndex("TemplateVersionId");
                b.ToTable("ValidationResults");
            });

            modelBuilder.Entity("Edp.Template.Domain.Entities.TemplateVersion", b =>
            {
                b.HasOne("Edp.Template.Domain.Entities.Template").WithMany().HasForeignKey("TemplateId").OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity("Edp.Template.Domain.Entities.Placeholder", b =>
            {
                b.HasOne("Edp.Template.Domain.Entities.TemplateVersion").WithMany().HasForeignKey("TemplateVersionId").OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity("Edp.Template.Domain.Entities.ValidationResultEntity", b =>
            {
                b.HasOne("Edp.Template.Domain.Entities.TemplateVersion").WithMany().HasForeignKey("TemplateVersionId").OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
