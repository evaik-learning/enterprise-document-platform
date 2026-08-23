using Edp.Document.Domain.Exceptions;
using Edp.SharedKernel.Entities;

namespace Edp.Document.Domain.Entities;

public sealed class DocumentFile : AuditableEntity<Guid>
{
    public Guid DocumentId { get; private set; }
    public Guid DocumentVersionId { get; private set; }
    public string FileType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public byte[]? RowVersion { get; private set; }

    private DocumentFile()
    {
    }

    public static DocumentFile Create(
        Guid documentId,
        Guid documentVersionId,
        string fileType,
        string fileName,
        string contentType,
        long size,
        string storagePath)
    {
        if (documentId == Guid.Empty)
        {
            throw new DocumentDomainException("DocumentId is required.");
        }

        if (documentVersionId == Guid.Empty)
        {
            throw new DocumentDomainException("DocumentVersionId is required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fileType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        return new DocumentFile
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            DocumentVersionId = documentVersionId,
            FileType = fileType,
            FileName = fileName,
            ContentType = contentType,
            Size = size,
            StoragePath = storagePath
        };
    }
}
