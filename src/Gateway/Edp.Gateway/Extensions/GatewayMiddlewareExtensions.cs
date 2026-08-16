using Edp.Gateway.Configuration;
using Edp.Gateway.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Gateway.Extensions;

public static class GatewayMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayPlatformMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
