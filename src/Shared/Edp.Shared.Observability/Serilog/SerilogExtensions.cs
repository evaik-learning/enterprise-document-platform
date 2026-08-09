using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Shared.Observability.Serilog;

public static class SerilogExtensions
{
    public static IServiceCollection AddSharedSerilog(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Serilog here or provide helpers to configure it from app startup.
        return services;
    }
}
