namespace Edp.Template.Api.Models;

public sealed class UpdateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Base64-encoded concurrency token previously returned by the API (<c>rowVersion</c>).</summary>
    public string RowVersion { get; set; } = string.Empty;
}
