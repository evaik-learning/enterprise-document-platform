using Edp.Document.Application.Interfaces;
using Edp.Document.Application.Services;
using Edp.Document.Domain.Entities;
using Edp.Document.Domain.Enums;
using Edp.Document.Domain.Events;
using Edp.Document.Domain.Exceptions;
using Xunit;
using DocumentEntity = global::Edp.Document.Domain.Entities.Document;
using DocumentVersionEntity = global::Edp.Document.Domain.Entities.DocumentVersion;
using DocumentFileEntity = global::Edp.Document.Domain.Entities.DocumentFile;
using DocumentGenerationJobEntity = global::Edp.Document.Domain.Entities.DocumentGenerationJob;

namespace Edp.Document.Tests;

public sealed class DocumentWorkflowIntegrationTests
{
    [Fact]
    public async Task GenerateDocumentWorkflowCompletesDocumentPersistsVersionsAndPublishesGeneratedEvent()
    {
        var organizationId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var document = DocumentEntity.Create(organizationId, "Contract", "Offer", "offer", templateId, "Test contract");

        var documentRepository = new InMemoryDocumentRepository(document);
        var versionRepository = new InMemoryDocumentVersionRepository();
        var fileRepository = new InMemoryDocumentFileRepository();
        var jobRepository = new InMemoryDocumentGenerationJobRepository();
        var generator = new FakeDocumentGenerator();
        var converter = new FakeDocumentConverter();
        var storage = new FakeDocumentStorage();
        var eventPublisher = new RecordingEventPublisher();

        var service = new DocumentGenerationService(
            documentRepository,
            versionRepository,
            fileRepository,
            jobRepository,
            generator,
            converter,
            storage,
            eventPublisher);

        var response = await service.GenerateAsync(
            organizationId,
            document.Id,
            document.Name,
            new Dictionary<string, object?> { ["CustomerName"] = "Contoso" },
            ["DOCX", "PDF"]);

        Assert.Equal("Completed", response.Status);
        Assert.Equal(DocumentStatus.Generated, documentRepository.Document.Status);
        Assert.Equal(1, versionRepository.Versions.Count);
        Assert.Equal(2, fileRepository.Files.Count);
        Assert.Contains(fileRepository.Files, x => x.FileType == "DOCX");
        Assert.Contains(fileRepository.Files, x => x.FileType == "PDF");
        Assert.Contains(eventPublisher.Published, x => x is DocumentGeneratedDomainEvent);
        Assert.Equal(1, jobRepository.Jobs.Count);
        Assert.Equal("Completed", jobRepository.Jobs.Single().Status);
    }

    private sealed class InMemoryDocumentRepository : IDocumentRepository
    {
        public DocumentEntity Document { get; private set; }

        public InMemoryDocumentRepository(DocumentEntity document)
        {
            Document = document;
        }

        public Task<DocumentEntity?> GetByIdAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentEntity?>(Document.Id == documentId && Document.OrganizationId == organizationId ? Document : null);

        public Task<IReadOnlyList<DocumentEntity>> ListAsync(Guid organizationId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentEntity>>(new[] { Document });

        public Task AddAsync(DocumentEntity document, CancellationToken cancellationToken = default)
        {
            Document = document;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DocumentEntity document, CancellationToken cancellationToken = default)
        {
            Document = document;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryDocumentVersionRepository : IDocumentVersionRepository
    {
        public List<DocumentVersionEntity> Versions { get; } = [];

        public Task<DocumentVersionEntity?> GetLatestAsync(Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentVersionEntity?>(Versions.LastOrDefault(v => v.DocumentId == documentId));

        public Task<IReadOnlyList<DocumentVersionEntity>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentVersionEntity>>(Versions.Where(v => v.DocumentId == documentId).ToList());

        public Task AddAsync(DocumentVersionEntity version, CancellationToken cancellationToken = default)
        {
            Versions.Add(version);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryDocumentFileRepository : IDocumentFileRepository
    {
        public List<DocumentFileEntity> Files { get; } = [];

        public Task AddAsync(DocumentFileEntity file, CancellationToken cancellationToken = default)
        {
            Files.Add(file);
            return Task.CompletedTask;
        }

        public Task<DocumentFileEntity?> GetByVersionIdAsync(Guid documentVersionId, string? fileType = null, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentFileEntity?>(Files.FirstOrDefault(x => x.DocumentVersionId == documentVersionId
                && (string.IsNullOrWhiteSpace(fileType) || x.FileType.Equals(fileType, StringComparison.OrdinalIgnoreCase))));
    }

    private sealed class InMemoryDocumentGenerationJobRepository : IDocumentGenerationJobRepository
    {
        public List<DocumentGenerationJobEntity> Jobs { get; } = [];

        public Task<DocumentGenerationJobEntity?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentGenerationJobEntity?>(Jobs.FirstOrDefault(x => x.Id == jobId));

        public Task<IReadOnlyList<DocumentGenerationJobEntity>> GetPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentGenerationJobEntity>>(Jobs.Where(x => x.Status == "Queued" || x.Status == "Processing").ToList());

        public Task<bool> TryStartProcessingAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            var job = Jobs.FirstOrDefault(x => x.Id == jobId);
            if (job is null || job.Status is "Processing" or "Completed" or "Failed")
            {
                return Task.FromResult(false);
            }

            job.MarkProcessing();
            return Task.FromResult(true);
        }

        public Task AddAsync(DocumentGenerationJobEntity job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DocumentGenerationJobEntity job, CancellationToken cancellationToken = default)
        {
            var existing = Jobs.FirstOrDefault(x => x.Id == job.Id);
            if (existing is null)
            {
                Jobs.Add(job);
                return Task.CompletedTask;
            }

            Jobs.Remove(existing);
            Jobs.Add(job);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentGenerator : IDocumentGenerator
    {
        public Task<Stream> GenerateAsync(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
        {
            var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, leaveOpen: true);
            writer.Write("Generated document");
            writer.Flush();
            ms.Position = 0;
            return Task.FromResult<Stream>(ms);
        }
    }

    private sealed class FakeDocumentConverter : IDocumentConverter
    {
        public Task<Stream> ConvertAsync(Stream source, string sourceFormat, string targetFormat, CancellationToken cancellationToken = default)
        {
            var output = new MemoryStream();
            using var writer = new StreamWriter(output, leaveOpen: true);
            writer.Write("Converted PDF");
            writer.Flush();
            output.Position = 0;
            return Task.FromResult<Stream>(output);
        }
    }

    private sealed class FakeDocumentStorage : IDocumentStorage
    {
        public Task<string> SaveAsync(Guid organizationId, Guid documentId, Guid versionId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
            => Task.FromResult($"organizations/{organizationId}/documents/{documentId}/versions/{versionId}/{fileName}");

        public Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream?>(new MemoryStream());
    }

    private sealed class RecordingEventPublisher : IEventPublisher
    {
        public List<object> Published { get; } = [];

        public Task PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : class
        {
            Published.Add(payload);
            return Task.CompletedTask;
        }
    }
}
