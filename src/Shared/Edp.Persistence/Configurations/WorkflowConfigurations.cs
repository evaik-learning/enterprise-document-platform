using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Edp.Workflow.Domain;
using WorkflowEntity = Edp.Workflow.Domain.Workflow;
using WorkflowIdempotencyRecord = Edp.Workflow.Application.Contracts.IdempotencyRecord;
using WorkflowInboxMessage = Edp.Workflow.Application.Contracts.InboxMessage;
using WorkflowOutboxMessage = Edp.Workflow.Application.Contracts.OutboxMessage;

namespace Edp.Persistence.Configurations;

public sealed class WorkflowConfiguration : IEntityTypeConfiguration<WorkflowEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowEntity> entity)
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
    }
}

public sealed class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowVersion> entity)
    {
        entity.ToTable("WorkflowVersions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.CreatedBy).HasMaxLength(200);
        entity.HasIndex(x => new { x.WorkflowId, x.Version }).IsUnique();
        entity.HasOne<WorkflowEntity>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowStateConfiguration : IEntityTypeConfiguration<WorkflowState>
{
    public void Configure(EntityTypeBuilder<WorkflowState> entity)
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
    }
}

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> entity)
    {
        entity.ToTable("WorkflowTransitions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.GuardJson).HasColumnType("nvarchar(max)");
        entity.HasIndex(x => new { x.WorkflowVersionId, x.FromStateId, x.Order });
        entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(x => x.WorkflowVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> entity)
    {
        entity.ToTable("WorkflowInstances");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.RejectionReason).HasMaxLength(2000);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => new { x.OrganizationId, x.Status });
        entity.HasIndex(x => new { x.OrganizationId, x.DocumentId });
    }
}

public sealed class WorkflowStateInstanceConfiguration : IEntityTypeConfiguration<WorkflowStateInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowStateInstance> entity)
    {
        entity.ToTable("WorkflowStateInstances");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.WorkflowInstanceId);
        entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowVariableConfiguration : IEntityTypeConfiguration<WorkflowVariable>
{
    public void Configure(EntityTypeBuilder<WorkflowVariable> entity)
    {
        entity.ToTable("WorkflowVariables");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired().HasMaxLength(150);
        entity.Property(x => x.Value).IsRequired().HasColumnType("nvarchar(max)");
        entity.Property(x => x.DataType).IsRequired().HasMaxLength(50);
        entity.HasIndex(x => new { x.WorkflowInstanceId, x.Name }).IsUnique();
        entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApprovalTaskConfiguration : IEntityTypeConfiguration<ApprovalTask>
{
    public void Configure(EntityTypeBuilder<ApprovalTask> entity)
    {
        entity.ToTable("ApprovalTasks");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => new { x.OrganizationId, x.AssignedToUserId, x.Status });
        entity.HasIndex(x => x.WorkflowInstanceId);
        entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApprovalActionConfiguration : IEntityTypeConfiguration<ApprovalAction>
{
    public void Configure(EntityTypeBuilder<ApprovalAction> entity)
    {
        entity.ToTable("ApprovalActions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.Comment).HasMaxLength(2000);
        entity.HasIndex(x => x.ApprovalTaskId);
        entity.HasOne<ApprovalTask>().WithMany().HasForeignKey(x => x.ApprovalTaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowHistoryConfiguration : IEntityTypeConfiguration<WorkflowHistory>
{
    public void Configure(EntityTypeBuilder<WorkflowHistory> entity)
    {
        entity.ToTable("WorkflowHistory");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.EventType).HasConversion<string>().HasMaxLength(50);
        entity.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        entity.Property(x => x.Data).HasColumnType("nvarchar(max)");
        entity.HasIndex(x => new { x.OrganizationId, x.WorkflowInstanceId, x.EventAt });
        entity.HasOne<WorkflowInstance>().WithMany().HasForeignKey(x => x.WorkflowInstanceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowOutboxConfiguration : IEntityTypeConfiguration<WorkflowOutboxMessage>
{
    public void Configure(EntityTypeBuilder<WorkflowOutboxMessage> entity)
    {
        entity.ToTable("OutboxMessages", "workflow");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.EventType).IsRequired().HasMaxLength(250);
        entity.Property(x => x.AggregateType).IsRequired().HasMaxLength(250);
        entity.Property(x => x.Payload).IsRequired().HasColumnType("nvarchar(max)");
        entity.Property(x => x.Error).HasMaxLength(4000);
        entity.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc }).HasDatabaseName("IX_WorkflowOutboxMessages_Pending");
    }
}

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<WorkflowIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<WorkflowIdempotencyRecord> entity)
    {
        entity.ToTable("IdempotencyRecords");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Key).IsRequired().HasMaxLength(200);
        entity.Property(x => x.RequestHash).IsRequired().HasMaxLength(128);
        entity.Property(x => x.ResponseBody).IsRequired().HasColumnType("nvarchar(max)");
        entity.HasIndex(x => new { x.OrganizationId, x.Key }).IsUnique();
    }
}

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<WorkflowInboxMessage>
{
    public void Configure(EntityTypeBuilder<WorkflowInboxMessage> entity)
    {
        entity.ToTable("InboxMessages");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.MessageId).IsRequired().HasMaxLength(200);
        entity.Property(x => x.EventType).IsRequired().HasMaxLength(250);
        entity.Property(x => x.Error).HasMaxLength(4000);
        entity.HasIndex(x => x.MessageId).IsUnique();
    }
}
