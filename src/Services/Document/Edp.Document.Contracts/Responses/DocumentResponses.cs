namespace Edp.Document.Contracts.Responses;

public class DocumentSummaryResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public int CurrentVersionNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DocumentDetailResponse : DocumentSummaryResponse
{
    public string? Description { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
    public IReadOnlyList<DocumentVersionResponse> Versions { get; set; } = Array.Empty<DocumentVersionResponse>();
}

public sealed class DocumentVersionResponse
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public int TemplateVersion { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public IReadOnlyList<DocumentFileResponse> Files { get; set; } = Array.Empty<DocumentFileResponse>();
}

public sealed class DocumentFileResponse
{
    public Guid Id { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

public sealed class DocumentGenerationResponse
{
    public Guid DocumentId { get; set; }
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ValidationIssueResponse
{
    public string Placeholder { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ValidationResponse
{
    public bool IsValid { get; set; }
    public List<ValidationIssueResponse> Errors { get; set; } = [];
}
