using Edp.Workflow.Domain;
using Edp.Workflow.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Edp.Workflow.Infrastructure.Persistence;

public sealed class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    public DbSet<global::Edp.Workflow.Domain.Workflow> Workflows => Set<global::Edp.Workflow.Domain.Workflow>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStateInstance> WorkflowStateInstances => Set<WorkflowStateInstance>();
    public DbSet<WorkflowVariable> WorkflowVariables => Set<WorkflowVariable>();
    public DbSet<ApprovalTask> ApprovalTasks => Set<ApprovalTask>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<WorkflowHistory> WorkflowHistory => Set<WorkflowHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<global::Edp.Workflow.Domain.Workflow>(entity =>
        {
            entity.ToTable("Workflows");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(250);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.CreatedBy).HasMaxLength(200);
            entity.Property(x => x.ModifiedBy).HasMaxLength(200);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<WorkflowVersion>(entity =>
        {
            entity.ToTable("WorkflowVersions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedBy).HasMaxLength(200);
            entity.HasIndex(x => new { x.WorkflowId, x.Version }).IsUnique();
            entity.HasOne<global::Edp.Workflow.Domain.Workflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowState>(entity =>
        {
            entity.ToTable("WorkflowStates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Configuration).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ApprovalPolicyJson).HasColumnType("nvarchar(max)");
            entity.Ignore(x => x.AssignmentRules);
            entity.Ignore(x => x.RequiredVariables);
            entity.HasIndex(x => new { x.WorkflowVersionId, x.Name }).IsUnique();
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(x => x.WorkflowVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowTransition>(entity =>
        {
            entity.ToTable("WorkflowTransitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.GuardJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.WorkflowVersionId, x.FromStateId, x.Order });
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(x => x.WorkflowVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("WorkflowInstances");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.RejectionReason).HasMaxLength(2000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.OrganizationId, x.Status });
            entity.HasIndex(x => new { x.OrganizationId, x.DocumentId });
        });

        modelBuilder.Entity<WorkflowStateInstance>(entity =>
        {
            entity.ToTable("WorkflowStateInstances");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WorkflowInstanceId);
            entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowVariable>(entity =>
        {
            entity.ToTable("WorkflowVariables");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Value).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(x => x.DataType).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => new { x.WorkflowInstanceId, x.Name }).IsUnique();
            entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalTask>(entity =>
        {
            entity.ToTable("ApprovalTasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.OrganizationId, x.AssignedToUserId, x.Status });
            entity.HasIndex(x => x.WorkflowInstanceId);
            entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalAction>(entity =>
        {
            entity.ToTable("ApprovalActions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.HasIndex(x => x.ApprovalTaskId);
            entity.HasOne<ApprovalTask>().WithMany().HasForeignKey(x => x.ApprovalTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowHistory>(entity =>
        {
            entity.ToTable("WorkflowHistory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.Data).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.OrganizationId, x.WorkflowInstanceId, x.EventAt });
            entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(250);
            entity.Property(x => x.AggregateType).IsRequired().HasMaxLength(250);
            entity.Property(x => x.Payload).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc }).HasDatabaseName("IX_WorkflowOutboxMessages_Pending");
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("IdempotencyRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).IsRequired().HasMaxLength(200);
            entity.Property(x => x.RequestHash).IsRequired().HasMaxLength(128);
            entity.Property(x => x.ResponseBody).IsRequired().HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.OrganizationId, x.Key }).IsUnique();
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MessageId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(250);
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.HasIndex(x => x.MessageId).IsUnique();
        });
    }
}