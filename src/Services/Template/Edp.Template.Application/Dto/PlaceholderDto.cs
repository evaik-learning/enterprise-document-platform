namespace Edp.Template.Application.Dto;

public sealed class PlaceholderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string DataType { get; set; } = "String";
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Format { get; set; }
    public string? Description { get; set; }
    public int Occurrences { get; set; }
}

public sealed class PlaceholderDiscoveryResultDto
{
    public Guid TemplateVersionId { get; set; }
    public IReadOnlyList<string> Discovered { get; set; } = [];
    public IReadOnlyList<string> NewPlaceholders { get; set; } = [];
    public IReadOnlyList<string> ExistingPlaceholders { get; set; } = [];
    public IReadOnlyList<string> MissingFromDocument { get; set; } = [];
    public string Status { get; set; } = "Consistent";
}
