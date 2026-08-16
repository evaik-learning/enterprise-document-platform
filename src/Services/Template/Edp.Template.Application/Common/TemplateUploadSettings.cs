namespace Edp.Template.Application.Common;

/// <summary>Configuration-driven upload constraints for template DOCX files.</summary>
public sealed class TemplateUploadSettings
{
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; init; } = [".docx"];
    public string[] AllowedContentTypes { get; init; } =
    [
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];
    public string BlobContainer { get; init; } = "templates";
}
