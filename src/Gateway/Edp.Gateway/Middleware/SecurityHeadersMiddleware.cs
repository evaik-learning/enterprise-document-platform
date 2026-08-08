using Edp.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeaderOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<GatewayOptions> options)
    {
        _next = next;
        _options = options.Value.SecurityHeaders;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", _options.ReferrerPolicy);
            headers.TryAdd("Permissions-Policy", _options.PermissionsPolicy);
            headers.TryAdd("Content-Security-Policy", _options.ContentSecurityPolicy);

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
