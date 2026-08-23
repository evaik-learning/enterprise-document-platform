using Edp.Shared.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Edp.Shared.Infrastructure.Middleware;

/// <summary>Converts known <see cref="ProblemDetailsException"/> types and unhandled exceptions into RFC 7807 responses.</summary>
public sealed class ProblemDetailsExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsExceptionMiddleware> _logger;

    public ProblemDetailsExceptionMiddleware(RequestDelegate next, ILogger<ProblemDetailsExceptionMiddleware> logger)
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
        catch (ProblemDetailsException exception)
        {
            _logger.LogWarning(exception, "Request failed with {Title}", exception.Title);
            await WriteProblemAsync(context, (int)exception.StatusCode, exception.Title, exception.Detail, exception.Code, exception.Errors);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception processing request");

            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "The request could not be completed.", "UNEXPECTED_ERROR", new[] { "The request could not be completed." });
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail, string code, IReadOnlyList<string> errors)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            code,
            message = detail,
            correlationId = context.TraceIdentifier,
            errors = errors.Select(error => new
            {
                code,
                message = error
            }).ToArray(),
            status = statusCode,
            title,
            instance = context.Request.Path,
            traceId = context.TraceIdentifier
        });
    }
}
