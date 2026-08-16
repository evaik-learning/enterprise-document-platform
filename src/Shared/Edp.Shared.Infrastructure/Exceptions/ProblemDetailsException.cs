using System.Net;

namespace Edp.Shared.Infrastructure.Exceptions;

public abstract class ProblemDetailsException : Exception
{
    protected ProblemDetailsException(string title, string detail, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(detail)
    {
        Title = title;
        Detail = detail;
        StatusCode = statusCode;
    }

    public string Title { get; }
    public string Detail { get; }
    public HttpStatusCode StatusCode { get; }
}

public sealed class ValidationProblemDetailsException : ProblemDetailsException
{
    public ValidationProblemDetailsException(string detail)
        : base("Validation failed", detail, HttpStatusCode.BadRequest)
    {
    }
}

public sealed class NotFoundProblemDetailsException : ProblemDetailsException
{
    public NotFoundProblemDetailsException(string detail)
        : base("Resource not found", detail, HttpStatusCode.NotFound)
    {
    }
}

public sealed class ForbiddenProblemDetailsException : ProblemDetailsException
{
    public ForbiddenProblemDetailsException(string detail)
        : base("Forbidden", detail, HttpStatusCode.Forbidden)
    {
    }
}
