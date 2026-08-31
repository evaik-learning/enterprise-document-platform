using Edp.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeaderOptions _options;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<GatewayOptions> options, IWebHostEnvironment environment)
    {
        _next = next;
        _options = options.Value.SecurityHeaders;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            var contentSecurityPolicy = _environment.IsDevelopment()
                ? _options.DevelopmentContentSecurityPolicy
                : _options.ContentSecurityPolicy;

            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", _options.ReferrerPolicy);
            headers.TryAdd("Permissions-Policy", _options.PermissionsPolicy);
            headers.TryAdd("Content-Security-Policy", contentSecurityPolicy);

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
