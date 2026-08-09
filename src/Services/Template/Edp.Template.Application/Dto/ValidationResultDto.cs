namespace Edp.Template.Application.Dto;

public sealed class ValidationResultDto
{
    public bool IsValid { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public IEnumerable<string>? Warnings { get; set; }
}
