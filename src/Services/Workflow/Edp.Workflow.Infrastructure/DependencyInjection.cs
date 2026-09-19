using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Application.Contracts;
using Edp.Workflow.Infrastructure.Persistence;
using Edp.Workflow.Infrastructure.BackgroundJobs;
using Edp.Workflow.Infrastructure.Messaging;
using Edp.Workflow.Infrastructure.Outbox;
using Edp.Shared.Messaging;
using Edp.Shared.Messaging.Abstractions;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Persistence;

namespace Edp.Workflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EdpDb")
            ?? throw new InvalidOperationException("Connection string 'EdpDb' is not configured.");

        services.AddDbContext<EdpDbContext>(options => options.UseSqlServer(connectionString));
        services.AddUnitOfWork<EdpDbContext>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowVersionRepository, WorkflowVersionRepository>();
        services.AddScoped<IWorkflowStateRepository, WorkflowStateRepository>();
        services.AddScoped<IWorkflowTransitionRepository, WorkflowTransitionRepository>();
        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IApprovalTaskRepository, ApprovalTaskRepository>();
        services.AddScoped<IWorkflowHistoryRepository, WorkflowHistoryRepository>();
        services.AddScoped<IOutboxMessageRepository, WorkflowOutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();
        services.AddSingleton<WorkflowMessageHandler>();
        services.AddSharedServiceBusPublisher(configuration, "ServiceBus:WorkflowTopic", "workflow-events");
        services.AddHostedService<OutboxBackgroundService>();
        services.AddHostedService<ApprovalTimeoutWorker>();
        services.AddHostedService<WorkflowSubscriptionHostedService>();

        return services;
    }
}
