namespace Edp.Template.Application.Dto;

public sealed class TemplateVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? FileHash { get; set; }
}
