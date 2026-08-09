namespace Edp.Template.Domain.Entities;

public sealed class ValidationResultEntity
{
    public Guid Id { get; init; }
    public Guid TemplateVersionId { get; init; }
    public string Status { get; set; } = string.Empty; // NotValidated, Valid, Invalid
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public DateTime ValidatedAt { get; set; }
}
