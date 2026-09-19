using Edp.DigitalSignature.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Edp.DigitalSignature.Api.Middleware;

public sealed class SigningDomainExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public SigningDomainExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (SigningDomainException exception) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = exception.HttpStatusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                code = exception.ErrorCode,
                message = exception.Message,
                errors = new[] { new { code = exception.ErrorCode, message = exception.Message } },
                status = exception.HttpStatusCode,
                title = "Signing request operation failed",
                instance = context.Request.Path,
                traceId = context.TraceIdentifier
            });
        }
        catch (DbUpdateConcurrencyException) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                code = "SIGNING_CONCURRENCY_CONFLICT",
                message = "The signing request was changed by another operation. Reload it and retry.",
                status = StatusCodes.Status409Conflict,
                title = "Signing request conflict",
                instance = context.Request.Path,
                traceId = context.TraceIdentifier
            });
        }
    }
}