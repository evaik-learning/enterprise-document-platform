namespace Edp.Template.Application.Dto;

public sealed class TemplateVersionDto
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ChangeDescription { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public IReadOnlyList<PlaceholderDto> Placeholders { get; set; } = [];
}
