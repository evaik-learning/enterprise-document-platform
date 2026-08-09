using Microsoft.Extensions.DependencyInjection;

namespace Edp.Shared.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        // Add common infrastructure registrations here
        return services;
    }
}
