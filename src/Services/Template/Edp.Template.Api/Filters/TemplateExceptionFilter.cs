using Edp.Template.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Edp.Template.Api.Filters;

/// <summary>Translates Template application-layer exceptions into RFC 7807 ProblemDetails responses.</summary>
public sealed class TemplateExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusCode, title) = context.Exception switch
        {
            TemplateNotFoundException => (StatusCodes.Status404NotFound, "Template not found"),
            TemplateVersionNotFoundException => (StatusCodes.Status404NotFound, "Template version not found"),
            TemplateCodeConflictException => (StatusCodes.Status409Conflict, "Template code conflict"),
            TemplateConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            TemplateOperationNotAllowedException => (StatusCodes.Status422UnprocessableEntity, "Operation not allowed"),
            TemplateFileValidationException => (StatusCodes.Status400BadRequest, "Invalid file"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message,
            Instance = context.HttpContext.Request.Path
        })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }
}
