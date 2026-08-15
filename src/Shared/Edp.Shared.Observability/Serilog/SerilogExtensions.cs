using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Edp.Shared.Observability.Serilog;

public static class SerilogExtensions
{
    public static IServiceCollection AddSharedSerilog(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        var logLevel = configuration["Logging:LogLevel:Default"];
        if (!string.IsNullOrWhiteSpace(logLevel) && Enum.TryParse<LogLevel>(logLevel, true, out var parsedLevel))
        {
            services.Configure<LoggerFilterOptions>(options =>
            {
                options.MinLevel = parsedLevel;
            });
        }

        return services;
    }
}
