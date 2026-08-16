namespace Edp.Template.Application.Dto;

public sealed class ValidationIssueDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public sealed class ValidationResultDto
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public IReadOnlyList<ValidationIssueDto> Errors { get; set; } = [];
    public IReadOnlyList<ValidationIssueDto> Warnings { get; set; } = [];
    public DateTimeOffset ValidatedAt { get; set; }
}
