using Edp.Audit.Domain.Entities;
using Edp.Document.Domain.Entities;
using Edp.Identity.Domain.Entities;
using Edp.Organization.Domain.Entities;
using Edp.Template.Domain.Entities;
using Edp.Workflow.Domain;
using Edp.Notification.Domain.Entities;
using NotificationEntity = Edp.Notification.Domain.Entities.Notification;
using Microsoft.EntityFrameworkCore;
using DocumentEntity = Edp.Document.Domain.Entities.Document;
using OrganizationEntity = Edp.Organization.Domain.Entities.Organization;
using TemplateEntity = Edp.Template.Domain.Entities.Template;
using WorkflowEntity = Edp.Workflow.Domain.Workflow;
using TemplateOutboxMessage = Edp.Template.Application.Contracts.OutboxMessage;
using WorkflowIdempotencyRecord = Edp.Workflow.Application.Contracts.IdempotencyRecord;
using WorkflowInboxMessage = Edp.Workflow.Application.Contracts.InboxMessage;
using WorkflowOutboxMessage = Edp.Workflow.Application.Contracts.OutboxMessage;
using DocumentOutboxMessage = Edp.Document.Application.Contracts.DocumentOutboxMessage;

namespace Edp.Persistence;

public sealed class EdpDbContext(DbContextOptions<EdpDbContext> options) : DbContext(options)
{
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<DocumentGenerationJob> DocumentGenerationJobs => Set<DocumentGenerationJob>();
    public DbSet<DocumentOutboxMessage> DocumentOutboxMessages => Set<DocumentOutboxMessage>();

    public DbSet<TemplateEntity> Templates => Set<TemplateEntity>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<Placeholder> Placeholders => Set<Placeholder>();
    public DbSet<ValidationResultEntity> ValidationResults => Set<ValidationResultEntity>();
    public DbSet<TemplateOutboxMessage> TemplateOutboxMessages => Set<TemplateOutboxMessage>();

    public DbSet<WorkflowEntity> Workflows => Set<WorkflowEntity>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStateInstance> WorkflowStateInstances => Set<WorkflowStateInstance>();
    public DbSet<WorkflowVariable> WorkflowVariables => Set<WorkflowVariable>();
    public DbSet<ApprovalTask> ApprovalTasks => Set<ApprovalTask>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<WorkflowHistory> WorkflowHistory => Set<WorkflowHistory>();
    public DbSet<WorkflowOutboxMessage> WorkflowOutboxMessages => Set<WorkflowOutboxMessage>();
    public DbSet<WorkflowIdempotencyRecord> IdempotencyRecords => Set<WorkflowIdempotencyRecord>();
    public DbSet<WorkflowInboxMessage> InboxMessages => Set<WorkflowInboxMessage>();
    public DbSet<NotificationInboxMessage> NotificationInboxMessages => Set<NotificationInboxMessage>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EdpDbContext).Assembly);
    }
}
