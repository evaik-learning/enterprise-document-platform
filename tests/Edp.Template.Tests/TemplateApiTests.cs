using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Security.CurrentUser;
using Edp.Shared.Storage.Abstractions;
using Edp.Template.Api.Controllers;
using Edp.Template.Api.Models;
using Edp.Template.Api.Security;
using Edp.Template.Application.Commands;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;
using Edp.Template.Application.Interfaces;
using Edp.Template.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Edp.Template.Tests;

public class TemplateApiTests
{
    [Fact]
    public async Task CreateWhenOrganizationContextMissingThrowsForbiddenProblemDetailsException()
    {
        var service = new FakeTemplateService();
        var currentUser = new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var currentOrganization = new CurrentOrganization { OrganizationId = null };
        var controller = new TemplatesController(service, currentUser, currentOrganization);

        var ex = await Assert.ThrowsAsync<Edp.Shared.Infrastructure.Exceptions.ForbiddenProblemDetailsException>(
            () => controller.Create(new CreateTemplateRequest { Name = "Offer", Code = "OFFER" }, CancellationToken.None));

        Assert.Equal("An organization context is required to access templates.", ex.Detail);
    }

    [Fact]
    public async Task CreateWhenOrganizationContextPresentReturnsCreatedAtAction()
    {
        var service = new FakeTemplateService();
        var organizationId = Guid.NewGuid();
        var currentUser = new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true };
        var currentOrganization = new CurrentOrganization { OrganizationId = organizationId };
        var controller = new TemplatesController(service, currentUser, currentOrganization);

        var result = await controller.Create(new CreateTemplateRequest { Name = "Offer", Code = "OFFER", Description = "Test" }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TemplatesController.Get), created.ActionName);
        Assert.NotNull(created.RouteValues);
        Assert.Equal(service.TemplateId, created.RouteValues!["templateId"]);
    }

    [Fact]
    public async Task UploadVersionWhenFileMissingReturnsBadRequest()
    {
        var service = new FakeTemplateService();
        var organizationId = Guid.NewGuid();
        var controller = new TemplatesController(service, new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true }, new CurrentOrganization { OrganizationId = organizationId });

        var request = new UploadTemplateVersionRequest
        {
            File = null!
        };

        var result = await controller.UploadVersion(Guid.NewGuid(), request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A non-empty file is required.", badRequest.Value);
    }

    [Fact]
    public async Task CreatePlaceholderWhenOrganizationContextPresentReturnsCreatedAtAction()
    {
        var service = new FakeTemplateService();
        var organizationId = Guid.NewGuid();
        var controller = new PlaceholderController(service, new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true }, new CurrentOrganization { OrganizationId = organizationId });

        var result = await controller.Create(Guid.NewGuid(), Guid.NewGuid(), new CreatePlaceholderRequest
        {
            Name = "CustomerName",
            DisplayName = "Customer Name",
            DataType = "String",
            IsRequired = true,
            Description = "Customer legal name"
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PlaceholderController.Get), created.ActionName);
    }

    [Fact]
    public async Task UpdatePlaceholderWhenPlaceholderExistsReturnsUpdatedPlaceholder()
    {
        var service = new FakeTemplateService();
        var organizationId = Guid.NewGuid();
        var controller = new PlaceholderController(service, new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true }, new CurrentOrganization { OrganizationId = organizationId });

        var result = await controller.Update(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new UpdatePlaceholderRequest
        {
            DisplayName = "Updated Customer Name",
            IsRequired = false,
            Description = "Updated description"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PlaceholderDto>(ok.Value);
        Assert.Equal("Updated Customer Name", dto.DisplayName);
        Assert.False(dto.IsRequired);
    }

    [Fact]
    public async Task DiscoverWhenDocumentAndDefinitionsDifferReturnsReviewSummary()
    {
        var service = new FakeTemplateService();
        var organizationId = Guid.NewGuid();
        var controller = new PlaceholderController(service, new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true }, new CurrentOrganization { OrganizationId = organizationId });

        var result = await controller.Discover(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var discovery = Assert.IsType<PlaceholderDiscoveryResultDto>(ok.Value);
        Assert.Contains("CustomerName", discovery.Discovered);
        Assert.Contains("LegacyField", discovery.MissingFromDocument);
        Assert.Equal("RequiresReview", discovery.Status);
    }

    [Fact]
    public async Task ValidatePlaceholdersWhenDocumentHasMissingDefinitionReturnsError()
    {
        var service = new FakeTemplateService();
        var organizationId = Guid.NewGuid();
        var controller = new PlaceholderController(service, new CurrentUser { UserId = Guid.NewGuid(), IsAuthenticated = true }, new CurrentOrganization { OrganizationId = organizationId });

        var result = await controller.ValidatePlaceholders(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var validation = Assert.IsType<ValidationResultDto>(ok.Value);
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, issue => issue.Code == "PLH201");
    }

    [Fact]
    public void AddSharedAuthorizationRegistersPoliciesThatRequireAuthenticatedUser()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharedAuthorization("PolicyA", "PolicyB");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy("PolicyA"));
        Assert.NotNull(options.GetPolicy("PolicyB"));
        Assert.Contains(options.GetPolicy("PolicyA")!.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
        Assert.Contains(options.GetPolicy("PolicyB")!.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void AddTemplateAuthorizationRegistersTemplatePoliciesAndBearerDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTemplateAuthorization();

        using var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        Assert.Equal("Bearer", authenticationOptions.DefaultAuthenticateScheme);
        Assert.Equal("Bearer", authenticationOptions.DefaultChallengeScheme);

        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateRead));
        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateCreate));
        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateUpdate));
        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateUpload));
        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateValidate));
        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateActivate));
        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateDeactivate));
        Assert.NotNull(authOptions.GetPolicy(TemplateAuthorizationPolicies.TemplateArchive));
    }

    [Fact]
    public async Task CreateAsyncRecordsAuditLog()
    {
        var templates = new FakeTemplateRepository();
        var versions = new FakeTemplateVersionRepository();
        var placeholders = new FakePlaceholderRepository();
        var validationResults = new FakeValidationResultRepository();
        var storage = new FakeBlobStorageService();
        var extractor = new FakePlaceholderExtractor();
        var validator = new FakeTemplateValidator();
        var eventPublisher = new FakeIntegrationEventPublisher();
        var auditLogger = new FakeAuditLogger();
        var service = new TemplateService(templates, versions, placeholders, validationResults, storage, extractor, validator, eventPublisher, auditLogger, new Edp.Template.Application.Common.TemplateUploadSettings
        {
            MaxFileSizeBytes = 10 * 1024 * 1024,
            AllowedExtensions = [".docx"],
            AllowedContentTypes = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            BlobContainer = "templates"
        });

        await service.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateTemplateCommand("Offer", "OFFER", "Test template"), CancellationToken.None);

        Assert.Single(auditLogger.Records);
        Assert.Equal("Create", auditLogger.Records[0].Action);
        Assert.Equal("Template", auditLogger.Records[0].EntityType);
    }

    private sealed class FakeTemplateRepository : ITemplateRepository
    {
        private readonly List<global::Edp.Template.Domain.Entities.Template> _items = [];

        public Task<global::Edp.Template.Domain.Entities.Template?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(t => t.OrganizationId == organizationId && t.Id == id));

        public Task<bool> CodeExistsAsync(Guid organizationId, string code, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(t => t.OrganizationId == organizationId && string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(global::Edp.Template.Domain.Entities.Template template, CancellationToken cancellationToken = default)
        {
            _items.Add(template);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(global::Edp.Template.Domain.Entities.Template template, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(t => t.Id == template.Id);
            if (index >= 0)
            {
                _items[index] = template;
            }

            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)> SearchAsync(
            Guid organizationId,
            string? search = null,
            string? status = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var items = _items
                .Where(t => t.OrganizationId == organizationId)
                .ToList();

            return Task.FromResult<(IReadOnlyList<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)>((items, items.Count));
        }
    }

    private sealed class FakeTemplateVersionRepository : ITemplateVersionRepository
    {
        private readonly List<global::Edp.Template.Domain.Entities.TemplateVersion> _items = [];

        public Task AddAsync(global::Edp.Template.Domain.Entities.TemplateVersion version, CancellationToken cancellationToken = default)
        {
            _items.Add(version);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(global::Edp.Template.Domain.Entities.TemplateVersion version, CancellationToken cancellationToken = default)
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

        public Task<IReadOnlyList<global::Edp.Template.Domain.Entities.TemplateVersion>> GetByTemplateIdAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<global::Edp.Template.Domain.Entities.TemplateVersion>>(_items.Where(v => v.OrganizationId == organizationId && v.TemplateId == templateId).ToList());

        public Task<global::Edp.Template.Domain.Entities.TemplateVersion?> GetByIdAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(v => v.OrganizationId == organizationId && v.TemplateId == templateId && v.Id == versionId));

        public Task<IReadOnlyList<global::Edp.Template.Domain.Entities.TemplateVersion>> GetActiveVersionsAsync(Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<global::Edp.Template.Domain.Entities.TemplateVersion>>(_items.Where(v => v.TemplateId == templateId && v.Status == global::Edp.Template.Domain.Enums.TemplateVersionStatus.Active).ToList());
    }

    private sealed class FakePlaceholderRepository : IPlaceholderRepository
    {
        private readonly List<global::Edp.Template.Domain.Entities.Placeholder> _items = [];

        public Task AddAsync(global::Edp.Template.Domain.Entities.Placeholder placeholder, CancellationToken cancellationToken = default)
        {
            _items.Add(placeholder);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<global::Edp.Template.Domain.Entities.Placeholder> placeholders, CancellationToken cancellationToken = default)
        {
            _items.AddRange(placeholders);
            return Task.CompletedTask;
        }

        public Task<global::Edp.Template.Domain.Entities.Placeholder?> GetByIdAsync(Guid placeholderId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(p => p.Id == placeholderId));

        public Task<global::Edp.Template.Domain.Entities.Placeholder?> GetByVersionAndIdAsync(Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(p => p.TemplateVersionId == versionId && p.Id == placeholderId));

        public Task<IReadOnlyList<global::Edp.Template.Domain.Entities.Placeholder>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<global::Edp.Template.Domain.Entities.Placeholder>>(_items.Where(p => p.TemplateVersionId == versionId).ToList());

        public Task UpdateAsync(global::Edp.Template.Domain.Entities.Placeholder placeholder, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(p => p.Id == placeholder.Id);
            if (index >= 0)
            {
                _items[index] = placeholder;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(global::Edp.Template.Domain.Entities.Placeholder placeholder, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(p => p.Id == placeholder.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeValidationResultRepository : IValidationResultRepository
    {
        private readonly List<global::Edp.Template.Domain.Entities.ValidationResultEntity> _items = [];

        public Task AddAsync(global::Edp.Template.Domain.Entities.ValidationResultEntity result, CancellationToken cancellationToken = default)
        {
            _items.Add(result);
            return Task.CompletedTask;
        }

        public Task<global::Edp.Template.Domain.Entities.ValidationResultEntity?> GetLatestByVersionIdAsync(Guid templateVersionId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Where(r => r.TemplateVersionId == templateVersionId).OrderByDescending(r => r.ValidatedAt).FirstOrDefault());
    }

    private sealed class FakeBlobStorageService : IBlobStorageService
    {
        public Task<string> UploadAsync(Stream content, string path, CancellationToken cancellationToken = default) => Task.FromResult(path);
        public Task<string> UploadAsync(Stream content, string path, string? contentType = null, CancellationToken cancellationToken = default) => Task.FromResult(path);
        public Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(new MemoryStream([1, 2, 3, 4]));
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class FakePlaceholderExtractor : IPlaceholderExtractor
    {
        public Task<IEnumerable<PlaceholderDto>> ExtractAsync(Stream docxStream, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<PlaceholderDto>>([]);
    }

    private sealed class FakeTemplateValidator : ITemplateValidator
    {
        public Task<ValidationResultDto> ValidateAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ValidationResultDto
            {
                IsValid = true,
                Status = "Valid",
                ErrorCount = 0,
                WarningCount = 0,
                Errors = [],
                Warnings = []
            });
    }

    private sealed class FakeIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync(global::Edp.SharedKernel.Domain.DomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishRangeAsync(IEnumerable<global::Edp.SharedKernel.Domain.DomainEvent> domainEvents, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAuditLogger : ITemplateAuditLogger
    {
        public List<AuditRecord> Records { get; } = [];

        public Task RecordAsync(Guid organizationId, Guid? userId, string action, string entityType, Guid entityId, Dictionary<string, object?>? metadata = null, CancellationToken cancellationToken = default)
        {
            Records.Add(new AuditRecord(organizationId, userId, action, entityType, entityId, metadata));
            return Task.CompletedTask;
        }
    }

    public sealed record AuditRecord(Guid OrganizationId, Guid? UserId, string Action, string EntityType, Guid EntityId, Dictionary<string, object?>? Metadata);

    private sealed class FakeTemplateService : ITemplateService
    {
        public Guid TemplateId { get; } = Guid.NewGuid();

        public Task<TemplateDto> CreateAsync(Guid organizationId, Guid? userId, CreateTemplateCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(new TemplateDto
            {
                Id = TemplateId,
                OrganizationId = organizationId,
                Name = command.Name,
                Code = command.Code,
                Status = "Draft",
                RowVersion = string.Empty
            });

        public Task<TemplateDto?> GetAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<TemplateDto?>(new TemplateDto { Id = templateId, OrganizationId = organizationId, Name = "Offer", Code = "OFFER", Status = "Draft", RowVersion = string.Empty });

        public Task<PagedResult<TemplateDto>> ListAsync(Guid organizationId, string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<TemplateDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize });

        public Task<TemplateDto> UpdateAsync(Guid organizationId, Guid? userId, Guid templateId, UpdateTemplateCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(new TemplateDto { Id = templateId, OrganizationId = organizationId, Name = command.Name, Code = "OFFER", Status = "Draft", RowVersion = string.Empty });

        public Task<TemplateVersionDto> UploadVersionAsync(Guid organizationId, Guid? userId, Guid templateId, Stream content, string fileName, string contentType, long fileSize, string? changeDescription, CancellationToken cancellationToken = default)
            => Task.FromResult(new TemplateVersionDto { Id = Guid.NewGuid(), TemplateId = templateId, FileName = fileName, ContentType = contentType, StoragePath = "test-path", Status = "Uploaded", ValidationStatus = "NotValidated", Placeholders = [] });

        public Task<IReadOnlyList<TemplateVersionDto>> GetVersionsAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TemplateVersionDto>>([]);

        public Task<TemplateVersionDto?> GetVersionAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<TemplateVersionDto?>(null);

        public Task<(Stream Content, string ContentType, string FileName)?> DownloadVersionAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<(Stream Content, string ContentType, string FileName)?>((null!, "application/octet-stream", "test.docx"));

        public Task<PlaceholderDto?> GetPlaceholderAsync(Guid organizationId, Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default)
            => Task.FromResult<PlaceholderDto?>(new PlaceholderDto { Id = placeholderId, Name = "CustomerName", DisplayName = "Customer Name", DataType = "String", IsRequired = true, Description = "Customer legal name" });

        public Task<PlaceholderDto> CreatePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CreatePlaceholderCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(new PlaceholderDto { Id = Guid.NewGuid(), Name = command.Name, DisplayName = command.DisplayName ?? command.Name, DataType = command.DataType ?? "String", IsRequired = command.IsRequired ?? true, Description = command.Description });

        public Task<PlaceholderDto> UpdatePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, Guid placeholderId, UpdatePlaceholderCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(new PlaceholderDto { Id = placeholderId, Name = "CustomerName", DisplayName = command.DisplayName ?? "Customer Name", DataType = command.DataType ?? "String", IsRequired = command.IsRequired ?? false, Description = command.Description ?? "Updated description" });

        public Task<bool> DeletePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<IReadOnlyList<PlaceholderDto>> GetPlaceholdersAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PlaceholderDto>>([]);

        public Task<ValidationResultDto> ValidateVersionAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ValidationResultDto { IsValid = true, Status = "Valid", Errors = [], Warnings = [] });

        public Task<PlaceholderDiscoveryResultDto> DiscoverPlaceholdersAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new PlaceholderDiscoveryResultDto
            {
                TemplateVersionId = versionId,
                Discovered = ["CustomerName", "Address"],
                NewPlaceholders = ["Address"],
                ExistingPlaceholders = ["CustomerName"],
                MissingFromDocument = ["LegacyField"],
                Status = "RequiresReview"
            });

        public Task<ValidationResultDto> ValidatePlaceholdersAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ValidationResultDto
            {
                IsValid = false,
                Status = "RequiresReview",
                ErrorCount = 1,
                WarningCount = 0,
                Errors =
                [
                    new ValidationIssueDto { Code = "PLH201", Severity = "Error", Message = "Definition exists but placeholder is missing from the document." }
                ],
                Warnings = []
            });

        public Task<ValidationResultDto?> GetValidationResultAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult<ValidationResultDto?>(null);

        public Task<TemplateDto> ActivateVersionAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new TemplateDto { Id = templateId, OrganizationId = organizationId, Name = "Offer", Code = "OFFER", Status = "Active", RowVersion = string.Empty });

        public Task<TemplateDto> DeactivateAsync(Guid organizationId, Guid? userId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult(new TemplateDto { Id = templateId, OrganizationId = organizationId, Name = "Offer", Code = "OFFER", Status = "Draft", RowVersion = string.Empty });

        public Task<TemplateDto> ArchiveAsync(Guid organizationId, Guid? userId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult(new TemplateDto { Id = templateId, OrganizationId = organizationId, Name = "Offer", Code = "OFFER", Status = "Archived", RowVersion = string.Empty });
    }
}
