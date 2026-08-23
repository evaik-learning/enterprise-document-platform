namespace Edp.Document.Contracts.Requests;

public sealed class CreateDocumentRequest
{
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "Contract";
    public string? Description { get; set; }
    public Guid TemplateId { get; set; }
    public int TemplateVersion { get; set; } = 1;
    public Dictionary<string, object?> Data { get; set; } = new();
}

public sealed class GenerateDocumentRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> OutputFormats { get; set; } = ["DOCX", "PDF"];
    public Dictionary<string, object?> Data { get; set; } = new();
}

public sealed class ListDocumentsRequest
{
    public string? Status { get; set; }
    public string? TemplateId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
