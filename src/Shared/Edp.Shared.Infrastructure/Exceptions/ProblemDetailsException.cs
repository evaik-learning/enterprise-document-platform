using System.Net;

namespace Edp.Shared.Infrastructure.Exceptions;

public abstract class ProblemDetailsException : Exception
{
    protected ProblemDetailsException(string title, string detail, HttpStatusCode statusCode = HttpStatusCode.BadRequest, string? code = null, IEnumerable<string>? errors = null)
        : base(detail)
    {
        Title = title;
        Detail = detail;
        StatusCode = statusCode;
        Code = NormalizeCode(code ?? title);
        Errors = (errors ?? new[] { detail }).ToArray();
    }

    public string Title { get; }
    public string Detail { get; }
    public HttpStatusCode StatusCode { get; }
    public string Code { get; }
    public IReadOnlyList<string> Errors { get; }

    private static string NormalizeCode(string value)
    {
        var normalized = new List<char>();
        foreach (var ch in value.Trim())
        {
            if (char.IsLetterOrDigit(ch))
            {
                normalized.Add(char.ToUpperInvariant(ch));
            }
            else if (normalized.Count > 0 && normalized[^1] != '_')
            {
                normalized.Add('_');
            }
        }

        var result = new string(normalized.ToArray()).Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "PROBLEM_DETAILS" : result;
    }
}

public sealed class ValidationProblemDetailsException : ProblemDetailsException
{
    public ValidationProblemDetailsException(string detail, string? code = null)
        : base("Validation failed", detail, HttpStatusCode.BadRequest, code ?? "VALIDATION_FAILED")
    {
    }
}

public sealed class NotFoundProblemDetailsException : ProblemDetailsException
{
    public NotFoundProblemDetailsException(string detail, string? code = null)
        : base("Resource not found", detail, HttpStatusCode.NotFound, code ?? "RESOURCE_NOT_FOUND")
    {
    }
}

public sealed class ForbiddenProblemDetailsException : ProblemDetailsException
{
    public ForbiddenProblemDetailsException(string detail, string? code = null)
        : base("Forbidden", detail, HttpStatusCode.Forbidden, code ?? "FORBIDDEN")
    {
    }
}

public sealed class ConflictProblemDetailsException : ProblemDetailsException
{
    public ConflictProblemDetailsException(string detail, string? code = null)
        : base("Conflict", detail, HttpStatusCode.Conflict, code ?? "CONFLICT")
    {
    }
}

public sealed class UnprocessableEntityProblemDetailsException : ProblemDetailsException
{
    public UnprocessableEntityProblemDetailsException(string detail, string? code = null)
        : base("Unprocessable entity", detail, HttpStatusCode.UnprocessableEntity, code ?? "UNPROCESSABLE_ENTITY")
    {
    }
}

public sealed class PayloadTooLargeProblemDetailsException : ProblemDetailsException
{
    public PayloadTooLargeProblemDetailsException(string detail, string? code = null)
        : base("Payload too large", detail, HttpStatusCode.RequestEntityTooLarge, code ?? "PAYLOAD_TOO_LARGE")
    {
    }
}
