namespace Edp.Template.Api.Models;

public sealed class CreatePlaceholderRequest
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? DataType { get; set; } = "String";
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Format { get; set; }
    public string? Description { get; set; }
    public int Occurrences { get; set; } = 1;
}
