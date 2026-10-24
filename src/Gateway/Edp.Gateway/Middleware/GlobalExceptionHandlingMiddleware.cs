using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled gateway exception");

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var correlationId = context.Response.Headers.TryGetValue("X-Correlation-ID", out var header) && !string.IsNullOrWhiteSpace(header)
                ? header.ToString()
                : context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "GATEWAY_UNEXPECTED_ERROR",
                message = "The gateway could not complete the request.",
                correlationId,
                details = Array.Empty<object>(),
                status = StatusCodes.Status500InternalServerError,
                title = "An unexpected error occurred.",
                detail = "The gateway could not complete the request.",
                instance = context.Request.Path.Value
            });
        }
    }
}
