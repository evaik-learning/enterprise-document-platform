using Microsoft.Extensions.DependencyInjection;
using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Application.Services;
using Edp.Workflow.Domain;
using Microsoft.Extensions.Configuration;

namespace Edp.Workflow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
            services.Configure<WorkflowOptions>(configuration.GetSection("Workflow"));
        else
            services.AddOptions<WorkflowOptions>();
        services.AddSingleton<IGuardEvaluator, GuardEvaluator>();
        services.AddScoped<IAssignmentResolver, AssignmentResolver>();
        services.AddScoped<IWorkflowStateMachine, WorkflowStateMachine>();
        services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();
        services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
        services.AddScoped<IApprovalTimeoutService, ApprovalTimeoutService>();
        return services;
    }
}
