namespace Edp.Template.Api.Models;

public sealed class UploadTemplateVersionRequest
{
    public IFormFile File { get; set; } = null!;
    public string? ChangeDescription { get; set; }
}
