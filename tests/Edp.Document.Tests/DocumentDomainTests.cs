using Edp.Document.Application.Interfaces;
using Edp.Document.Application.Services;
using Edp.Document.Contracts.Responses;
using Edp.Document.Domain.Entities;
using Edp.Document.Domain.Enums;
using Edp.Document.Domain.Exceptions;
using Edp.Document.Infrastructure.Templates;
using Xunit;
using DocumentEntity = Edp.Document.Domain.Entities.Document;

namespace Edp.Document.Tests;

public class DocumentDomainTests
{
    [Fact]
    public void CreateDocumentRequiresOrganization()
    {
        var ex = Assert.Throws<DocumentDomainException>(() =>
            DocumentEntity.Create(Guid.Empty, "Contract", "Contract A", "contract", Guid.NewGuid(), "desc"));

        Assert.Contains("OrganizationId", ex.Message);
    }

    [Fact]
    public void CreateDocumentSetsStatusAndVersion()
    {
        var document = DocumentEntity.Create(Guid.NewGuid(), "Contract", "Contract A", "contract", Guid.NewGuid(), "desc");

        Assert.Equal(DocumentStatus.Requested, document.Status);
        Assert.Equal(1, document.CurrentVersionNumber);
        Assert.NotEmpty(document.DomainEvents);
    }

    [Fact]
    public void GenerateVersionIncrementsVersionNumber()
    {
        var document = DocumentEntity.Create(Guid.NewGuid(), "Contract", "Contract A", "contract", Guid.NewGuid(), "desc");

        document.MarkGenerating();
        document.MarkGenerated(2, Guid.NewGuid());

        Assert.Equal(2, document.CurrentVersionNumber);
        Assert.Equal(DocumentStatus.Generated, document.Status);
    }

    [Fact]
    public void CanTransitionToArchivedState()
    {
        var document = DocumentEntity.Create(Guid.NewGuid(), "Contract", "Contract A", "contract", Guid.NewGuid(), "desc");
        document.MarkGenerating();
        document.MarkGenerated(2, Guid.NewGuid());

        document.Archive();

        Assert.Equal(DocumentStatus.Archived, document.Status);
    }

    [Fact]
    public void CannotArchiveBeforeGenerationCompletes()
    {
        var document = DocumentEntity.Create(Guid.NewGuid(), "Contract", "Contract A", "contract", Guid.NewGuid(), "desc");

        var ex = Assert.Throws<DocumentDomainException>(() => document.Archive());

        Assert.Contains("Generated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CannotMarkGeneratedFromRequestedState()
    {
        var document = DocumentEntity.Create(Guid.NewGuid(), "Contract", "Contract A", "contract", Guid.NewGuid(), "desc");

        var ex = Assert.Throws<DocumentDomainException>(() => document.MarkGenerated(2, Guid.NewGuid()));

        Assert.Contains("Generating", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanFailGenerationAfterGenerationStarts()
    {
        var document = DocumentEntity.Create(Guid.NewGuid(), "Contract", "Contract A", "contract", Guid.NewGuid(), "desc");
        document.MarkGenerating();

        document.FailGeneration("Template unavailable");

        Assert.Equal(DocumentStatus.Failed, document.Status);
    }

    [Fact]
    public async Task GenerateAsyncIgnoresDuplicateRequestsForAlreadyGeneratedDocuments()
    {
        var organizationId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var document = DocumentEntity.Create(organizationId, "Contract", "Contract A", "contract", templateId, "desc");
        document.MarkGenerating();
        document.MarkGenerated(1, Guid.NewGuid());

        var documentRepository = new FakeDocumentRepository(document);
        var versionRepository = new FakeDocumentVersionRepository();
        var fileRepository = new FakeDocumentFileRepository();
        var jobRepository = new FakeDocumentGenerationJobRepository();
        var generator = new FakeDocumentGenerator();
        var converter = new FakeDocumentConverter();
        var storage = new FakeDocumentStorage();
        var publisher = new FakeEventPublisher();
        var service = new DocumentGenerationService(documentRepository, versionRepository, fileRepository, jobRepository, generator, converter, storage, publisher);

        var response = await service.GenerateAsync(organizationId, document.Id, "Contract A", new Dictionary<string, object?>
        {
            ["CustomerName"] = "Contoso"
        }, new List<string> { "DOCX", "PDF" });

        Assert.Equal("Completed", response.Status);
        Assert.Empty(versionRepository.Versions);
        Assert.Empty(fileRepository.Files);
    }

    [Fact]
    public async Task TryStartProcessingAsyncClaimsOnlyOneWorkerForSameJob()
    {
        var job = DocumentGenerationJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var repository = new FakeDocumentGenerationJobRepository(job);

        var firstClaim = await repository.TryStartProcessingAsync(job.Id);
        var secondClaim = await repository.TryStartProcessingAsync(job.Id);

        Assert.True(firstClaim);
        Assert.False(secondClaim);
        Assert.Equal("Processing", repository.Jobs.Single().Status);
    }

    [Fact]
    public void RetryableFailuresAreQueuedForRetryAndTaggedAsTransient()
    {
        var job = DocumentGenerationJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        job.MarkProcessing();

        job.MarkRetryScheduled("Temporary blob storage failure.", "Transient");

        Assert.Equal("Queued", job.Status);
        Assert.Equal("Transient", job.FailureCategory);
        Assert.Equal(1, job.Attempts);
    }

    [Fact]
    public void NonRetryableFailuresAreMarkedAsFailedWithValidationCategory()
    {
        var job = DocumentGenerationJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        job.MarkProcessing();

        job.MarkFailed("Validation failed: missing placeholder.", "Validation");

        Assert.Equal("Failed", job.Status);
        Assert.Equal("Validation", job.FailureCategory);
        Assert.Contains("missing placeholder", job.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlaceholderValidationTreatsBlankValuesAsMissing()
    {
        var service = new PlaceholderResolutionService();

        var validation = service.Validate("Customer Contract", new Dictionary<string, object?>
        {
            ["CustomerName"] = "   ",
            ["ContractNumber"] = "CNT-1001"
        });

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, x => x.Placeholder == "CustomerName");
    }

    [Fact]
    public void DocumentInfrastructureContainsInitialDatabaseMigration()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EnterpriseDocumentPlatform.sln")))
        {
            directory = directory.Parent;
        }

        var migrationDirectory = Path.Combine(directory!.FullName, "src", "Services", "Document", "Edp.Document.Infrastructure", "Migrations");

        Assert.True(Directory.Exists(migrationDirectory));
        Assert.Contains(Directory.GetFiles(migrationDirectory), path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && path.Contains("InitialCreate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TemplateClientReturnsTemplateMetadataForGeneration()
    {
        var client = new TemplateServiceClient();

        var template = client.GetTemplateAsync(Guid.NewGuid(), Guid.NewGuid()).GetAwaiter().GetResult();

        Assert.NotNull(template);
        Assert.Contains("status", template!.GetType().GetProperties().Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsyncCompletesDocumentAndSetsJobStatus()
    {
        var organizationId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var document = DocumentEntity.Create(organizationId, "Contract", "Contract A", "contract", templateId, "desc");
        var documentRepository = new FakeDocumentRepository(document);
        var versionRepository = new FakeDocumentVersionRepository();
        var fileRepository = new FakeDocumentFileRepository();
        var jobRepository = new FakeDocumentGenerationJobRepository();
        var generator = new FakeDocumentGenerator();
        var converter = new FakeDocumentConverter();
        var storage = new FakeDocumentStorage();
        var publisher = new FakeEventPublisher();
        var service = new DocumentGenerationService(documentRepository, versionRepository, fileRepository, jobRepository, generator, converter, storage, publisher);

        var response = await service.GenerateAsync(organizationId, document.Id, "Contract A", new Dictionary<string, object?>
        {
            ["CustomerName"] = "Contoso"
        });

        Assert.Equal("Completed", response.Status);
        Assert.Equal(DocumentStatus.Generated, documentRepository.Document.Status);
        Assert.Equal("Completed", jobRepository.Jobs.Single().Status);
        Assert.False(string.IsNullOrWhiteSpace(jobRepository.Jobs.Single().CorrelationId));
        Assert.Equal(2, versionRepository.Versions.Single().VersionNumber);
        Assert.True(storage.Saved);
    }

    [Fact]
    public async Task GenerateAsyncMarksJobFailedWhenGenerationThrows()
    {
        var organizationId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var document = DocumentEntity.Create(organizationId, "Contract", "Contract A", "contract", templateId, "desc");
        var documentRepository = new FakeDocumentRepository(document);
        var versionRepository = new FakeDocumentVersionRepository();
        var fileRepository = new FakeDocumentFileRepository();
        var jobRepository = new FakeDocumentGenerationJobRepository();
        var generator = new ThrowingDocumentGenerator();
        var converter = new FakeDocumentConverter();
        var storage = new FakeDocumentStorage();
        var publisher = new FakeEventPublisher();
        var service = new DocumentGenerationService(documentRepository, versionRepository, fileRepository, jobRepository, generator, converter, storage, publisher);

        var ex = await Assert.ThrowsAsync<DocumentDomainException>(() => service.GenerateAsync(organizationId, document.Id, "Contract A", new Dictionary<string, object?>
        {
            ["CustomerName"] = "Contoso"
        }));

        Assert.Contains("Template unavailable", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Failed", jobRepository.Jobs.Single().Status);
        Assert.Equal(DocumentStatus.Failed, documentRepository.Document.Status);
    }

    [Fact]
    public async Task GenerateAsyncCreatesPdfOutputWhenRequested()
    {
        var organizationId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var document = DocumentEntity.Create(organizationId, "Contract", "Contract A", "contract", templateId, "desc");
        var documentRepository = new FakeDocumentRepository(document);
        var versionRepository = new FakeDocumentVersionRepository();
        var fileRepository = new FakeDocumentFileRepository();
        var jobRepository = new FakeDocumentGenerationJobRepository();
        var generator = new FakeDocumentGenerator();
        var converter = new FakeDocumentConverter();
        var storage = new FakeDocumentStorage();
        var publisher = new FakeEventPublisher();
        var service = new DocumentGenerationService(documentRepository, versionRepository, fileRepository, jobRepository, generator, converter, storage, publisher);

        var response = await service.GenerateAsync(organizationId, document.Id, "Contract A", new Dictionary<string, object?>
        {
            ["CustomerName"] = "Contoso"
        }, ["DOCX", "PDF"]);

        Assert.Equal("Completed", response.Status);
        Assert.Equal(2, fileRepository.Files.Count);
        Assert.Contains(fileRepository.Files, x => x.ContentType == "application/pdf");
        Assert.True(converter.Converted);
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public DocumentEntity Document { get; set; }

        public FakeDocumentRepository(DocumentEntity document)
        {
            Document = document;
        }

        public Task<DocumentEntity?> GetByIdAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentEntity?>(Document);

        public Task<IReadOnlyList<DocumentEntity>> ListAsync(Guid organizationId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentEntity>>(new[] { Document });

        public Task AddAsync(DocumentEntity document, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(DocumentEntity document, CancellationToken cancellationToken = default)
        {
            Document = document;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentVersionRepository : IDocumentVersionRepository
    {
        public List<DocumentVersion> Versions { get; } = new();

        public Task<DocumentVersion?> GetLatestAsync(Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentVersion?>(Versions.LastOrDefault());

        public Task<IReadOnlyList<DocumentVersion>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentVersion>>(Versions);

        public Task AddAsync(DocumentVersion version, CancellationToken cancellationToken = default)
        {
            Versions.Add(version);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentFileRepository : IDocumentFileRepository
    {
        public List<DocumentFile> Files { get; } = new();

        public Task AddAsync(DocumentFile file, CancellationToken cancellationToken = default)
        {
            Files.Add(file);
            return Task.CompletedTask;
        }

        public Task<DocumentFile?> GetByVersionIdAsync(Guid documentVersionId, string? fileType = null, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentFile?>(Files.FirstOrDefault(x => x.DocumentVersionId == documentVersionId
                && (string.IsNullOrWhiteSpace(fileType) || x.FileType.Equals(fileType, StringComparison.OrdinalIgnoreCase))));
    }

    private sealed class FakeDocumentGenerationJobRepository : IDocumentGenerationJobRepository
    {
        public List<DocumentGenerationJob> Jobs { get; } = new();

        public FakeDocumentGenerationJobRepository(params DocumentGenerationJob[] jobs)
        {
            Jobs.AddRange(jobs);
        }

        public Task<DocumentGenerationJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentGenerationJob?>(Jobs.FirstOrDefault(x => x.Id == jobId));

        public Task<IReadOnlyList<DocumentGenerationJob>> GetPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentGenerationJob>>(Jobs.Where(x => x.Status == "Queued" || x.Status == "Processing").ToList());

        public Task<bool> TryStartProcessingAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            var job = Jobs.FirstOrDefault(x => x.Id == jobId);
            if (job is null || job.Status == "Completed" || job.Status == "Failed" || job.Status == "Processing")
            {
                return Task.FromResult(false);
            }

            job.MarkProcessing();
            return Task.FromResult(true);
        }

        public Task AddAsync(DocumentGenerationJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DocumentGenerationJob job, CancellationToken cancellationToken = default)
        {
            var existing = Jobs.FirstOrDefault(x => x.Id == job.Id);
            if (existing is not null)
            {
                Jobs.Remove(existing);
                Jobs.Add(job);
            }
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentGenerator : IDocumentGenerator
    {
        public Task<Stream> GenerateAsync(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.Write("Generated");
            writer.Flush();
            stream.Position = 0;
            return Task.FromResult<Stream>(stream);
        }
    }

    private sealed class ThrowingDocumentGenerator : IDocumentGenerator
    {
        public Task<Stream> GenerateAsync(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Template unavailable");
    }

    private sealed class FakeDocumentStorage : IDocumentStorage
    {
        public bool Saved { get; private set; }

        public Task<string> SaveAsync(Guid organizationId, Guid documentId, Guid versionId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            Saved = true;
            return Task.FromResult($"organizations/{organizationId}/documents/{documentId}/versions/{versionId}/{fileName}");
        }

        public Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream?>(new MemoryStream());
    }

    private sealed class FakeDocumentConverter : IDocumentConverter
    {
        public bool Converted { get; private set; }

        public Task<Stream> ConvertAsync(Stream source, string sourceFormat, string targetFormat, CancellationToken cancellationToken = default)
        {
            Converted = true;
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.Write("PDF");
            writer.Flush();
            stream.Position = 0;
            return Task.FromResult<Stream>(stream);
        }
    }

    private sealed class FakeEventPublisher : IEventPublisher
    {
        public Task PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : class
            => Task.CompletedTask;
    }
}
