using Microsoft.AspNetCore.Builder;

namespace Edp.Shared.Infrastructure.Middleware;

public static class SharedMiddlewareExtensions
{
    public static IApplicationBuilder UseSharedPlatformMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}
