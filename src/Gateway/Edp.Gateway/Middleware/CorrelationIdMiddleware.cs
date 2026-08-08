using Edp.Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;

    public CorrelationIdMiddleware(RequestDelegate next, IOptions<GatewayOptions> options)
    {
        _next = next;
        _headerName = options.Value.Correlation.HeaderName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(_headerName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : context.TraceIdentifier;

        context.Items[_headerName] = correlationId;
        context.Response.Headers[_headerName] = correlationId;

        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>()
            .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
