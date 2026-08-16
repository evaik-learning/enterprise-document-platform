using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Edp.Shared.Infrastructure.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string DefaultHeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(DefaultHeaderName, out var incomingValue) && !string.IsNullOrWhiteSpace(incomingValue)
            ? incomingValue.ToString()
            : context.TraceIdentifier;

        context.Items[DefaultHeaderName] = correlationId;
        context.Response.Headers[DefaultHeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
