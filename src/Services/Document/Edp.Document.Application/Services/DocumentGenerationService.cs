using Edp.Document.Application.Interfaces;
using Edp.Document.Contracts.Responses;
using Edp.Document.Domain.Entities;
using Edp.Document.Domain.Enums;
using Edp.Document.Domain.Exceptions;

namespace Edp.Document.Application.Services;

public sealed class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _documentVersionRepository;
    private readonly IDocumentFileRepository _documentFileRepository;
    private readonly IDocumentGenerationJobRepository _documentGenerationJobRepository;
    private readonly IDocumentGenerator _documentGenerator;
    private readonly IDocumentConverter _documentConverter;
    private readonly IDocumentStorage _documentStorage;
    private readonly IEventPublisher _eventPublisher;

    public DocumentGenerationService(
        IDocumentRepository documentRepository,
        IDocumentVersionRepository documentVersionRepository,
        IDocumentFileRepository documentFileRepository,
        IDocumentGenerationJobRepository documentGenerationJobRepository,
        IDocumentGenerator documentGenerator,
        IDocumentConverter documentConverter,
        IDocumentStorage documentStorage,
        IEventPublisher eventPublisher)
    {
        _documentRepository = documentRepository;
        _documentVersionRepository = documentVersionRepository;
        _documentFileRepository = documentFileRepository;
        _documentGenerationJobRepository = documentGenerationJobRepository;
        _documentGenerator = documentGenerator;
        _documentConverter = documentConverter;
        _documentStorage = documentStorage;
        _eventPublisher = eventPublisher;
    }

    public async Task<DocumentGenerationResponse> GenerateAsync(Guid organizationId, Guid documentId, string documentName, Dictionary<string, object?> data, List<string>? outputFormats = null, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(organizationId, documentId, cancellationToken)
            ?? throw new DocumentDomainException($"Document '{documentId}' was not found for organization '{organizationId}'.");

        if (document.Status is DocumentStatus.Generated or DocumentStatus.Completed)
        {
            return new DocumentGenerationResponse
            {
                DocumentId = document.Id,
                JobId = Guid.Empty,
                Status = "Completed",
                Message = $"Document '{documentName}' has already been generated."
            };
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var job = DocumentGenerationJob.Create(documentId, organizationId, document.TemplateId, correlationId);
        await _documentGenerationJobRepository.AddAsync(job, cancellationToken);

        try
        {
            document.MarkGenerating();
            await _documentRepository.UpdateAsync(document, cancellationToken);
            job.MarkProcessing();
            await _documentGenerationJobRepository.UpdateAsync(job, cancellationToken);

            var generatedDocxStream = await _documentGenerator.GenerateAsync(organizationId, document.TemplateId, data, cancellationToken);
            var docxPayload = CloneStream(generatedDocxStream);
            var versionNumber = document.CurrentVersionNumber + 1;
            var versionId = Guid.NewGuid();
            var requestedFormats = (outputFormats ?? new List<string> { "DOCX" })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedFormats.Count == 0)
            {
                requestedFormats.Add("DOCX");
            }

            var fileNameBase = $"{documentName}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            var generatedVersion = DocumentVersion.Create(
                versionId,
                documentId,
                versionNumber,
                document.TemplateId,
                document.CurrentVersionNumber,
                $"{fileNameBase}.docx",
                $"organizations/{organizationId}/documents/{documentId}/versions/{versionId}/{fileNameBase}.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                docxPayload.Length);

            await _documentVersionRepository.AddAsync(generatedVersion, cancellationToken);

            foreach (var requestedFormat in requestedFormats)
            {
                var format = requestedFormat.Trim();
                Stream outputStream;
                string fileName;
                string contentType;
                string fileType;

                if (string.Equals(format, "PDF", StringComparison.OrdinalIgnoreCase))
                {
                    outputStream = await _documentConverter.ConvertAsync(new MemoryStream(docxPayload.ToArray()), "docx", "pdf", cancellationToken);
                    fileName = $"{fileNameBase}.pdf";
                    contentType = "application/pdf";
                    fileType = "PDF";
                }
                else
                {
                    outputStream = new MemoryStream(docxPayload.ToArray());
                    fileName = $"{fileNameBase}.docx";
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    fileType = "DOCX";
                }

                var storagePath = await _documentStorage.SaveAsync(organizationId, documentId, versionId, fileName, outputStream, contentType, cancellationToken);
                var documentFile = DocumentFile.Create(documentId, versionId, fileType, fileName, contentType, outputStream.Length, storagePath);
                await _documentFileRepository.AddAsync(documentFile, cancellationToken);
            }

            document.MarkGenerated(versionNumber, Guid.NewGuid());
            await _documentRepository.UpdateAsync(document, cancellationToken);

            job.MarkCompleted();
            await _documentGenerationJobRepository.UpdateAsync(job, cancellationToken);

            await _eventPublisher.PublishAsync(new global::Edp.Document.Domain.Events.DocumentGeneratedDomainEvent(document.Id, organizationId, versionNumber, Guid.NewGuid()), cancellationToken);

            return new DocumentGenerationResponse
            {
                DocumentId = document.Id,
                JobId = job.Id,
                Status = "Completed",
                Message = $"Document '{documentName}' was generated and stored successfully."
            };
        }
        catch (Exception ex) when (ex is not DocumentDomainException)
        {
            document.FailGeneration(ex.Message);
            await _documentRepository.UpdateAsync(document, cancellationToken);
            job.MarkFailed(ex.Message);
            await _documentGenerationJobRepository.UpdateAsync(job, cancellationToken);
            throw new DocumentDomainException($"Template unavailable: {ex.Message}");
        }
        catch (DocumentDomainException)
        {
            job.MarkFailed("Document generation failed.");
            await _documentGenerationJobRepository.UpdateAsync(job, cancellationToken);
            throw;
        }
    }

    private static MemoryStream CloneStream(Stream source)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        var buffer = new byte[source.Length];
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = source.Read(buffer, totalRead, buffer.Length - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return new MemoryStream(buffer, 0, totalRead, writable: false, publiclyVisible: false);
    }
}
