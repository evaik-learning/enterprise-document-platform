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

public sealed class ConflictProblemDetailsException : ProblemDetailsException
{
    public ConflictProblemDetailsException(string detail)
        : base("Conflict", detail, HttpStatusCode.Conflict)
    {
    }
}

public sealed class UnprocessableEntityProblemDetailsException : ProblemDetailsException
{
    public UnprocessableEntityProblemDetailsException(string detail)
        : base("Unprocessable entity", detail, HttpStatusCode.UnprocessableEntity)
    {
    }
}

public sealed class PayloadTooLargeProblemDetailsException : ProblemDetailsException
{
    public PayloadTooLargeProblemDetailsException(string detail)
        : base("Payload too large", detail, HttpStatusCode.RequestEntityTooLarge)
    {
    }
}
