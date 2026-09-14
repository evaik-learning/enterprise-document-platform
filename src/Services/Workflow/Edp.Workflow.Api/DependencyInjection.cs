using Microsoft.Extensions.DependencyInjection;

namespace Edp.Workflow.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowApi(this IServiceCollection services)
    {
        return services;
    }
}
