using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Edp.Shared.Infrastructure.Exceptions;
using Edp.Shared.Storage.Abstractions;
using Edp.SharedKernel.Domain;
using Edp.Template.Api.Controllers;
using Edp.Template.Api.Security;
using Edp.Template.Infrastructure.Document;
using Edp.Template.Application.Commands;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;
using Edp.Template.Application.Exceptions;
using Edp.Template.Application.Interfaces;
using Edp.Template.Application.Services;
using Edp.Template.Domain.Entities;
using Edp.Template.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Edp.Template.Tests;

public class TemplateAdvancedBehaviorTests
{
    [Fact]
    public void ControllersAndServiceExposeExpectedTemplateContract()
    {
        var templateController = typeof(TemplatesController);
        var placeholderController = typeof(PlaceholderController);
        var service = typeof(ITemplateService);

        Assert.Equal("api/v1/templates", templateController.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal("api/v1/templates/{templateId:guid}/versions/{versionId:guid}/placeholders", placeholderController.GetCustomAttribute<RouteAttribute>()?.Template);

        var requiredMethods = new[]
        {
            nameof(ITemplateService.CreateAsync),
            nameof(ITemplateService.UploadVersionAsync),
            nameof(ITemplateService.ValidateVersionAsync),
            nameof(ITemplateService.DiscoverPlaceholdersAsync),
            nameof(ITemplateService.ValidatePlaceholdersAsync),
            nameof(ITemplateService.ActivateVersionAsync),
            nameof(ITemplateService.ArchiveAsync)
        };

        foreach (var methodName in requiredMethods)
        {
            Assert.Contains(service.GetMethods(), method => method.Name == methodName);
        }

        Assert.Contains(templateController.GetMethods(), method => method.Name == nameof(TemplatesController.UploadVersion));
        Assert.Contains(placeholderController.GetMethods(), method => method.Name == nameof(PlaceholderController.Discover));
        Assert.Contains(placeholderController.GetMethods(), method => method.Name == nameof(PlaceholderController.ValidatePlaceholders));
    }

    [Fact]
    public async Task Service_CreateAndGet_IsTenantScoped()
    {
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var service = CreateService();
        var template = await service.CreateAsync(organizationA, userId, new CreateTemplateCommand("Offer", "OFFER", "Test"));

        var inTenant = await service.GetAsync(organizationA, template.Id);
        var crossTenant = await service.GetAsync(organizationB, template.Id);

        Assert.NotNull(inTenant);
        Assert.Null(crossTenant);
    }

    [Fact]
    public async Task Service_UpdateAsync_WhenRowVersionMismatch_ThrowsConcurrencyConflict()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();
        var template = await service.CreateAsync(Guid.NewGuid(), userId, new CreateTemplateCommand("Offer", "OFFER", "Test"));

        var rowVersion = new byte[] { 9, 9, 9, 9 };
        var ex = await Assert.ThrowsAsync<TemplateConcurrencyConflictException>(() =>
            service.UpdateAsync(template.OrganizationId, userId, template.Id, new UpdateTemplateCommand("Updated", "Updated description", rowVersion)));

        Assert.Contains("modified by another user", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Service_DiscoverAndValidatePlaceholders_ReportsDocumentDefinitionMismatch()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService();

        var template = await service.CreateAsync(organizationId, userId, new CreateTemplateCommand("Offer", "OFFER", "Test"));
        var stream = CreateDocxStream("{{CustomerName}}");
        var version = await service.UploadVersionAsync(
            organizationId,
            userId,
            template.Id,
            stream,
            "offer.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            1024,
            "Initial draft");

        await service.CreatePlaceholderAsync(organizationId, userId, template.Id, version.Id, new CreatePlaceholderCommand("LegacyField", "Legacy Field", "String", true, null, null, "Legacy", 1));

        var discovery = await service.DiscoverPlaceholdersAsync(organizationId, userId, template.Id, version.Id);
        var validation = await service.ValidatePlaceholdersAsync(organizationId, userId, template.Id, version.Id);

        Assert.Equal("RequiresReview", discovery.Status);
        Assert.Contains(discovery.MissingFromDocument, name => name == "LegacyField");
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, issue => issue.Code == "PLH201");
    }

    [Fact]
    public async Task Service_ValidatePlaceholders_WhenDefinitionsAndDocumentAreConsistent_IsValid()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService();

        var template = await service.CreateAsync(organizationId, userId, new CreateTemplateCommand("Offer", "OFFER", "Test"));
        var stream = CreateDocxStream("{{CustomerName}} {{InvoiceDate}}");
        var version = await service.UploadVersionAsync(
            organizationId,
            userId,
            template.Id,
            stream,
            "offer.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            1024,
            "Initial draft");


        var discovery = await service.DiscoverPlaceholdersAsync(organizationId, userId, template.Id, version.Id);
        var validation = await service.ValidatePlaceholdersAsync(organizationId, userId, template.Id, version.Id);

        Assert.Equal("Consistent", discovery.Status);
        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task OpenXmlExtractor_DetectsSamePlaceholderSetAcrossRepeatedRuns()
    {
        var extractor = new OpenXmlPlaceholderExtractor();
        var first = (await extractor.ExtractAsync(CreateDocxStream("{{CustomerName}} {{InvoiceDate}} {{CustomerName}}"))).ToList();
        var second = (await extractor.ExtractAsync(CreateDocxStream("{{CustomerName}} {{InvoiceDate}} {{CustomerName}}"))).ToList();

        var firstNames = first.Select(x => x.Name).Distinct().OrderBy(x => x).ToArray();
        var secondNames = second.Select(x => x.Name).Distinct().OrderBy(x => x).ToArray();

        Assert.Equal(new[] { "CustomerName", "InvoiceDate" }, firstNames);
        Assert.Equal(firstNames, secondNames);
        Assert.Equal(2, firstNames.Length);
    }

    [Fact]
    public async Task TemplateService_ActivateVersion_RequiresValidValidationResult()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService();

        var template = await service.CreateAsync(organizationId, userId, new CreateTemplateCommand("Offer", "OFFER", "Test"));
        var stream = CreateDocxStream("{{CustomerName}}");
        var version = await service.UploadVersionAsync(organizationId, userId, template.Id, stream, "offer.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 1024, "Initial");

        var ex = await Assert.ThrowsAsync<TemplateOperationNotAllowedException>(() =>
            service.ActivateVersionAsync(organizationId, userId, template.Id, version.Id));

        Assert.Contains("passed validation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static TemplateService CreateService()
    {
        var templates = new InMemoryTemplateRepository();
        var versions = new InMemoryTemplateVersionRepository();
        var placeholders = new InMemoryPlaceholderRepository();
        var validationResults = new InMemoryValidationResultRepository();
        var storage = new InMemoryBlobStorageService();
        var extractor = new StaticPlaceholderExtractor(
            new PlaceholderDto { Name = "CustomerName", DisplayName = "Customer Name", DataType = nameof(PlaceholderDataType.String), IsRequired = true, Occurrences = 1 },
            new PlaceholderDto { Name = "InvoiceDate", DisplayName = "Invoice Date", DataType = nameof(PlaceholderDataType.Date), IsRequired = true, Occurrences = 1 });
        var validator = new StaticTemplateValidator();
        var publisher = new FakeIntegrationEventPublisher();
        var auditLogger = new FakeAuditLogger();

        return new TemplateService(
            templates,
            versions,
            placeholders,
            validationResults,
            storage,
            extractor,
            validator,
            publisher,
            auditLogger,
            new Edp.Template.Application.Common.TemplateUploadSettings
            {
                MaxFileSizeBytes = 10 * 1024 * 1024,
                AllowedExtensions = [".docx"],
                AllowedContentTypes = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
                BlobContainer = "templates"
            });
    }

    private static MemoryStream CreateDocxStream(string text)
    {
        var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = package.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class InMemoryTemplateRepository : ITemplateRepository
    {
        private readonly List<global::Edp.Template.Domain.Entities.Template> _items = [];

        public Task<global::Edp.Template.Domain.Entities.Template?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(t => t.OrganizationId == organizationId && t.Id == id));

        public Task<bool> CodeExistsAsync(Guid organizationId, string code, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(t => t.OrganizationId == organizationId && t.Code == code));

        public Task AddAsync(global::Edp.Template.Domain.Entities.Template templateEntity, CancellationToken cancellationToken = default)
        {
            typeof(global::Edp.Template.Domain.Entities.Template)
                .GetProperty(nameof(global::Edp.Template.Domain.Entities.Template.RowVersion))!
                .SetValue(templateEntity, new byte[] { 1, 2, 3, 4 });

            _items.Add(templateEntity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(global::Edp.Template.Domain.Entities.Template templateEntity, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(t => t.Id == templateEntity.Id);
            if (index >= 0)
            {
                _items[index] = templateEntity;
            }

            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)> SearchAsync(Guid organizationId, string? search = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var items = _items.Where(t => t.OrganizationId == organizationId).ToList();
            return Task.FromResult<(IReadOnlyList<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)>((items, items.Count));
        }
    }

    private sealed class InMemoryTemplateVersionRepository : ITemplateVersionRepository
    {
        private readonly List<TemplateVersion> _items = [];

        public Task AddAsync(TemplateVersion version, CancellationToken cancellationToken = default)
        {
            _items.Add(version);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TemplateVersion version, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(v => v.Id == version.Id);
            if (index >= 0)
            {
                _items[index] = version;
            }

            return Task.CompletedTask;
        }

        public Task<int> GetNextVersionNumberAsync(Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Count(v => v.TemplateId == templateId) + 1);

        public Task<IReadOnlyList<TemplateVersion>> GetByTemplateIdAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TemplateVersion>>(_items.Where(v => v.OrganizationId == organizationId && v.TemplateId == templateId).ToList());

        public Task<TemplateVersion?> GetByIdAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(v => v.OrganizationId == organizationId && v.TemplateId == templateId && v.Id == versionId));

        public Task<IReadOnlyList<TemplateVersion>> GetActiveVersionsAsync(Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TemplateVersion>>(_items.Where(v => v.TemplateId == templateId && v.Status == TemplateVersionStatus.Active).ToList());
    }

    private sealed class InMemoryPlaceholderRepository : IPlaceholderRepository
    {
        private readonly List<Placeholder> _items = [];

        public Task AddAsync(Placeholder placeholder, CancellationToken cancellationToken = default)
        {
            _items.Add(placeholder);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<Placeholder> placeholders, CancellationToken cancellationToken = default)
        {
            _items.AddRange(placeholders);
            return Task.CompletedTask;
        }

        public Task<Placeholder?> GetByIdAsync(Guid placeholderId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(p => p.Id == placeholderId));

        public Task<Placeholder?> GetByVersionAndIdAsync(Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(p => p.TemplateVersionId == versionId && p.Id == placeholderId));

        public Task<IReadOnlyList<Placeholder>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Placeholder>>(_items.Where(p => p.TemplateVersionId == versionId).ToList());

        public Task UpdateAsync(Placeholder placeholder, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(p => p.Id == placeholder.Id);
            if (index >= 0)
            {
                _items[index] = placeholder;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Placeholder placeholder, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(p => p.Id == placeholder.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryValidationResultRepository : IValidationResultRepository
    {
        private readonly List<ValidationResultEntity> _items = [];

        public Task AddAsync(ValidationResultEntity result, CancellationToken cancellationToken = default)
        {
            _items.Add(result);
            return Task.CompletedTask;
        }

        public Task<ValidationResultEntity?> GetLatestByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Where(r => r.TemplateVersionId == versionId).OrderByDescending(r => r.ValidatedAt).FirstOrDefault());
    }

    private sealed class InMemoryBlobStorageService : IBlobStorageService
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        public Task<string> UploadAsync(Stream content, string path, CancellationToken cancellationToken = default)
            => UploadAsync(content, path, null, cancellationToken);

        public async Task<string> UploadAsync(Stream content, string path, string? contentType = null, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            _files[path] = ms.ToArray();
            return path;
        }

        public Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!_files.TryGetValue(path, out var buffer))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new MemoryStream(buffer));
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            _files.Remove(path);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult(_files.ContainsKey(path));
    }

    private sealed class StaticPlaceholderExtractor(params PlaceholderDto[] placeholders) : IPlaceholderExtractor
    {
        public Task<IEnumerable<PlaceholderDto>> ExtractAsync(Stream docxStream, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<PlaceholderDto>>(placeholders);
    }

    private sealed class StaticTemplateValidator : ITemplateValidator
    {
        public Task<ValidationResultDto> ValidateAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ValidationResultDto
            {
                IsValid = true,
                Status = "Valid",
                ErrorCount = 0,
                WarningCount = 0,
                Errors = [],
                Warnings = [],
                ValidatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private sealed class FakeIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishRangeAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAuditLogger : ITemplateAuditLogger
    {
        public Task RecordAsync(
            Guid organizationId,
            Guid? userId,
            string action,
            string entityType,
            Guid entityId,
            Dictionary<string, object?>? metadata = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
