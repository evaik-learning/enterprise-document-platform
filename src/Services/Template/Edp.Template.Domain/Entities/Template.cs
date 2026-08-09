namespace Edp.Template.Domain.Entities;

public sealed class Template
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; set; } = string.Empty;
    public Guid? CurrentVersionId { get; set; }
    public byte[]? RowVersion { get; set; }
}
