using Edp.Template.Domain.Enums;

namespace Edp.Template.Domain.Entities;

/// <summary>Persisted outcome of running the validation pipeline against a template version.</summary>
public sealed class ValidationResultEntity
{
    public Guid Id { get; init; }
    public Guid TemplateVersionId { get; init; }
    public ValidationStatus Status { get; set; } = ValidationStatus.NotValidated;
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public string? ErrorsJson { get; set; }
    public string? WarningsJson { get; set; }
    public DateTimeOffset ValidatedAt { get; set; }

    public static ValidationResultEntity Create(Guid templateVersionId, bool isValid, int errorCount, int warningCount, string? errorsJson, string? warningsJson)
    {
        return new ValidationResultEntity
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = templateVersionId,
            Status = isValid ? ValidationStatus.Valid : ValidationStatus.Invalid,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            ErrorsJson = errorsJson,
            WarningsJson = warningsJson,
            ValidatedAt = DateTimeOffset.UtcNow
        };
    }
}
