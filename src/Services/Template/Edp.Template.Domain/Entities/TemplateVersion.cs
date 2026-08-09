namespace Edp.Template.Domain.Entities;

public sealed class TemplateVersion
{
    public Guid Id { get; init; }
    public Guid TemplateId { get; init; }
    public int VersionNumber { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string StoragePath { get; init; } = string.Empty;
    public string? FileHash { get; init; }
    public long FileSize { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ChangeDescription { get; init; }
    public byte[]? RowVersion { get; set; }
}
