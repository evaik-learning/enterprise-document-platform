using Edp.Document.Domain.Exceptions;
using Edp.SharedKernel.Entities;

namespace Edp.Document.Domain.Entities;

public sealed class DocumentVersion : AuditableEntity<Guid>
{
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Status { get; private set; } = "Generated";
    public Guid TemplateId { get; private set; }
    public int TemplateVersion { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string StoragePath { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long FileSize { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public List<DocumentFile> Files { get; private set; } = [];

    private DocumentVersion()
    {
    }

    public static DocumentVersion Create(
        Guid id,
        Guid documentId,
        int versionNumber,
        Guid templateId,
        int templateVersion,
        string fileName,
        string storagePath,
        string contentType,
        long fileSize)
    {
        if (documentId == Guid.Empty)
        {
            throw new DocumentDomainException("DocumentId is required.");
        }

        if (versionNumber < 1)
        {
            throw new DocumentDomainException("Version number must be at least 1.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return new DocumentVersion
        {
            Id = id,
            DocumentId = documentId,
            VersionNumber = versionNumber,
            TemplateId = templateId,
            TemplateVersion = templateVersion,
            GeneratedAt = DateTimeOffset.UtcNow,
            StoragePath = storagePath,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            Status = "Generated"
        };
    }
}

