namespace Edp.Template.Api.Models;

public sealed class UpdatePlaceholderRequest
{
    public string? DisplayName { get; set; }
    public string? DataType { get; set; }
    public bool? IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Format { get; set; }
    public string? Description { get; set; }
}
