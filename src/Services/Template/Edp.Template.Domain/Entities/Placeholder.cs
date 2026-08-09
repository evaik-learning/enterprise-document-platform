namespace Edp.Template.Domain.Entities;

public sealed class Placeholder
{
    public Guid Id { get; init; }
    public Guid TemplateVersionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? DataType { get; init; }
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public string? Format { get; init; }
    public string? Description { get; init; }
}
