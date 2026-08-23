using Edp.Document.Contracts.Requests;
using Edp.Document.Contracts.Responses;
using DocumentEntity = global::Edp.Document.Domain.Entities.Document;
using DocumentVersionEntity = global::Edp.Document.Domain.Entities.DocumentVersion;
using DocumentFileEntity = global::Edp.Document.Domain.Entities.DocumentFile;
using DocumentGenerationJobEntity = global::Edp.Document.Domain.Entities.DocumentGenerationJob;

namespace Edp.Document.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentSummaryResponse> CreateAsync(Guid organizationId, CreateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<DocumentDetailResponse?> GetAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentSummaryResponse>> ListAsync(Guid organizationId, ListDocumentsRequest request, CancellationToken cancellationToken = default);
    Task<DocumentGenerationResponse> GenerateAsync(Guid organizationId, Guid documentId, GenerateDocumentRequest request, CancellationToken cancellationToken = default);
}

public interface IPlaceholderResolutionService
{
    Dictionary<string, object?> Resolve(string templateName, Dictionary<string, object?> data);
    ValidationResponse Validate(string templateName, Dictionary<string, object?> data);
}

public interface IDocumentGenerationService
{
    Task<DocumentGenerationResponse> GenerateAsync(Guid organizationId, Guid documentId, string documentName, Dictionary<string, object?> data, List<string>? outputFormats = null, CancellationToken cancellationToken = default);
}

public interface IDocumentRepository
{
    Task<DocumentEntity?> GetByIdAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentEntity>> ListAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(DocumentEntity document, CancellationToken cancellationToken = default);
    Task UpdateAsync(DocumentEntity document, CancellationToken cancellationToken = default);
}

public interface IDocumentVersionRepository
{
    Task<DocumentVersionEntity?> GetLatestAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentVersionEntity>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task AddAsync(DocumentVersionEntity version, CancellationToken cancellationToken = default);
}

public interface IDocumentFileRepository
{
    Task AddAsync(DocumentFileEntity file, CancellationToken cancellationToken = default);
}

public interface IDocumentGenerationJobRepository
{
    Task<DocumentGenerationJobEntity?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentGenerationJobEntity>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<bool> TryStartProcessingAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task AddAsync(DocumentGenerationJobEntity job, CancellationToken cancellationToken = default);
    Task UpdateAsync(DocumentGenerationJobEntity job, CancellationToken cancellationToken = default);
}

public interface IDocumentTemplateClient
{
    Task<object?> GetTemplateAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default);
}

public interface IPlaceholderValidator
{
    ValidationResponse Validate(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default);
}

public interface IDocumentGenerator
{
    Task<Stream> GenerateAsync(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default);
}

public interface IDocumentConverter
{
    Task<Stream> ConvertAsync(Stream source, string sourceFormat, string targetFormat, CancellationToken cancellationToken = default);
}

public interface IDocumentStorage
{
    Task<string> SaveAsync(Guid organizationId, Guid documentId, Guid versionId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T payload, CancellationToken cancellationToken = default)
        where T : class;
}
