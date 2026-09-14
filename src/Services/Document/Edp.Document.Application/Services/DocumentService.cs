using Edp.Document.Application.Interfaces;
using Edp.Document.Contracts.Requests;
using Edp.Document.Contracts.Responses;
using Edp.Document.Domain.Entities;
using Edp.Document.Domain.Exceptions;
using DocumentEntity = global::Edp.Document.Domain.Entities.Document;
using DocumentVersionEntity = global::Edp.Document.Domain.Entities.DocumentVersion;

namespace Edp.Document.Application.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _documentVersionRepository;
    private readonly IDocumentFileRepository _documentFileRepository;
    private readonly IPlaceholderResolutionService _placeholderResolutionService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IDocumentStorage _documentStorage;
    private readonly IEventPublisher _eventPublisher;

    public DocumentService(
        IDocumentRepository documentRepository,
        IDocumentVersionRepository documentVersionRepository,
        IDocumentFileRepository documentFileRepository,
        IPlaceholderResolutionService placeholderResolutionService,
        IDocumentGenerationService documentGenerationService,
        IDocumentStorage documentStorage,
        IEventPublisher eventPublisher)
    {
        _documentRepository = documentRepository;
        _documentVersionRepository = documentVersionRepository;
        _documentFileRepository = documentFileRepository;
        _placeholderResolutionService = placeholderResolutionService;
        _documentGenerationService = documentGenerationService;
        _documentStorage = documentStorage;
        _eventPublisher = eventPublisher;
    }

    public async Task<DocumentSummaryResponse> CreateAsync(Guid organizationId, CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TemplateId == Guid.Empty)
        {
            throw new DocumentDomainException("TemplateId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DocumentDomainException("Document name is required.");
        }

        var document = DocumentEntity.Create(
            organizationId,
            request.DocumentType,
            request.Name,
            request.TemplateId.ToString(),
            request.TemplateId,
            request.Description);

        await _documentRepository.AddAsync(document, cancellationToken);
        await _eventPublisher.PublishAsync(new global::Edp.Document.Domain.Events.DocumentCreatedDomainEvent(document.Id, organizationId, request.TemplateId, document.Name), cancellationToken);

        return MapSummary(document);
    }

    public async Task<DocumentDetailResponse?> GetAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(organizationId, documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var versions = await _documentVersionRepository.GetByDocumentIdAsync(documentId, cancellationToken);

        return new DocumentDetailResponse
        {
            Id = document.Id,
            OrganizationId = document.OrganizationId,
            Name = document.Name,
            DocumentType = document.DocumentType,
            Status = document.Status.ToString(),
            TemplateId = document.TemplateId,
            CurrentVersionNumber = document.CurrentVersionNumber,
            CreatedAt = document.CreatedAt,
            Description = document.Description,
            Data = new Dictionary<string, object?>(),
            Versions = versions.Select(MapVersion).ToArray()
        };
    }

    public async Task<IReadOnlyList<DocumentSummaryResponse>> ListAsync(Guid organizationId, ListDocumentsRequest request, CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.ListAsync(organizationId, cancellationToken);
        var filtered = documents
            .Where(x => string.IsNullOrWhiteSpace(request.Status) || x.Status.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return filtered.Select(MapSummary).ToList();
    }

    public async Task<DocumentGenerationResponse> GenerateAsync(Guid organizationId, Guid documentId, GenerateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = _placeholderResolutionService.Validate(request.Name, request.Data);
        if (!validation.IsValid)
        {
            throw new DocumentDomainException(string.Join("; ", validation.Errors.Select(x => x.Message)));
        }

        _placeholderResolutionService.Resolve(request.Name, request.Data);
        var response = await _documentGenerationService.GenerateAsync(organizationId, documentId, request.Name, request.Data, request.OutputFormats, cancellationToken);
        return response;
    }

    public async Task<DocumentDownload?> DownloadAsync(Guid organizationId, Guid documentId, string? fileType = null, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(organizationId, documentId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var version = await _documentVersionRepository.GetLatestAsync(documentId, cancellationToken);
        if (version is null)
        {
            return null;
        }

        var file = await _documentFileRepository.GetByVersionIdAsync(version.Id, fileType, cancellationToken);
        if (file is null)
        {
            return null;
        }

        var content = await _documentStorage.DownloadAsync(file.StoragePath, cancellationToken);
        return content is null ? null : new DocumentDownload(content, file.FileName, file.ContentType);
    }

    private static DocumentSummaryResponse MapSummary(DocumentEntity document) => new()
    {
        Id = document.Id,
        OrganizationId = document.OrganizationId,
        Name = document.Name,
        DocumentType = document.DocumentType,
        Status = document.Status.ToString(),
        TemplateId = document.TemplateId,
        CurrentVersionNumber = document.CurrentVersionNumber,
        CreatedAt = document.CreatedAt
    };

    private static DocumentVersionResponse MapVersion(DocumentVersionEntity version) => new()
    {
        Id = version.Id,
        VersionNumber = version.VersionNumber,
        Status = version.Status,
        TemplateId = version.TemplateId,
        TemplateVersion = version.TemplateVersion,
        GeneratedAt = version.GeneratedAt,
        Files = version.Files.Select(f => new DocumentFileResponse
        {
            Id = f.Id,
            FileType = f.FileType,
            FileName = f.FileName,
            ContentType = f.ContentType,
            Size = f.Size,
            StoragePath = f.StoragePath
        }).ToArray()
    };
}
