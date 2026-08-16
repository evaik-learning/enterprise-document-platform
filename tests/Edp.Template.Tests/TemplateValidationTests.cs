using System.Text;
using Edp.Shared.Storage.Abstractions;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;
using Edp.Template.Domain.Entities;
using Edp.Template.Domain.Enums;
using Edp.Template.Infrastructure.Validation;
using Xunit;

namespace Edp.Template.Tests;

public class TemplateValidationTests
{
    [Fact]
    public async Task ValidateAsyncWhenTemplateFileMissingReturnsError()
    {
        var versionRepo = new FakeTemplateVersionRepository();
        var placeholderRepo = new FakePlaceholderRepository();
        var storage = new FakeBlobStorageService(exists: false);
        var validator = new TemplateValidator(versionRepo, placeholderRepo, storage);

        var versionId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        versionRepo.SetVersion(TemplateVersion.Create(
            versionId,
            templateId,
            organizationId,
            1,
            "test.docx",
            "templates",
            "missing/test.docx",
            "hash",
            1,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            null,
            Guid.NewGuid()));

        var result = await validator.ValidateAsync(organizationId, templateId, versionId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "TPL001");
    }

    [Fact]
    public async Task ValidateAsyncWhenPlaceholderNameIsInvalidReturnsError()
    {
        var versionRepo = new FakeTemplateVersionRepository();
        var placeholderRepo = new FakePlaceholderRepository(
            Placeholder.Create(Guid.NewGuid(), "invalid-name", 1));
        var storage = new FakeBlobStorageService(exists: true);
        var validator = new TemplateValidator(versionRepo, placeholderRepo, storage);

        var versionId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        versionRepo.SetVersion(TemplateVersion.Create(
            versionId,
            templateId,
            organizationId,
            1,
            "test.docx",
            "templates",
            "exists/test.docx",
            "hash",
            1,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            null,
            Guid.NewGuid()));

        var result = await validator.ValidateAsync(organizationId, templateId, versionId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "TPL002");
    }

    [Fact]
    public async Task ValidateAsyncWhenNoPlaceholdersExistAddsWarning()
    {
        var versionRepo = new FakeTemplateVersionRepository();
        var placeholderRepo = new FakePlaceholderRepository();
        var storage = new FakeBlobStorageService(exists: true);
        var validator = new TemplateValidator(versionRepo, placeholderRepo, storage);

        var versionId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        versionRepo.SetVersion(TemplateVersion.Create(
            versionId,
            templateId,
            organizationId,
            1,
            "test.docx",
            "templates",
            "exists/test.docx",
            "hash",
            1,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            null,
            Guid.NewGuid()));

        var result = await validator.ValidateAsync(organizationId, templateId, versionId);

        Assert.Contains(result.Warnings, w => w.Code == "TPL103");
    }

    private sealed class FakeTemplateVersionRepository : ITemplateVersionRepository
    {
        private TemplateVersion? _version;

        public void SetVersion(TemplateVersion version) => _version = version;

        public Task AddAsync(TemplateVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(TemplateVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> GetNextVersionNumberAsync(Guid templateId, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<IReadOnlyList<TemplateVersion>> GetByTemplateIdAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TemplateVersion>>([]);
        public Task<TemplateVersion?> GetByIdAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(_version is not null && _version.Id == versionId && _version.TemplateId == templateId && _version.OrganizationId == organizationId ? _version : null);
        public Task<IReadOnlyList<TemplateVersion>> GetActiveVersionsAsync(Guid templateId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TemplateVersion>>([]);
    }

    private sealed class FakePlaceholderRepository : IPlaceholderRepository
    {
        private readonly IReadOnlyList<Placeholder> _placeholders;

        public FakePlaceholderRepository(params Placeholder[] placeholders)
        {
            _placeholders = placeholders;
        }

        public Task AddAsync(Placeholder placeholder, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<Placeholder> placeholders, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Placeholder?> GetByIdAsync(Guid placeholderId, CancellationToken cancellationToken = default) => Task.FromResult(_placeholders.FirstOrDefault(p => p.Id == placeholderId));
        public Task<Placeholder?> GetByVersionAndIdAsync(Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default) => Task.FromResult(_placeholders.FirstOrDefault(p => p.TemplateVersionId == versionId && p.Id == placeholderId));
        public Task<IReadOnlyList<Placeholder>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default) => Task.FromResult(_placeholders);
        public Task UpdateAsync(Placeholder placeholder, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Placeholder placeholder, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeBlobStorageService : IBlobStorageService
    {
        private readonly bool _exists;

        public FakeBlobStorageService(bool exists) => _exists = exists;

        public Task<string> UploadAsync(Stream content, string path, CancellationToken cancellationToken = default) => Task.FromResult(path);
        public Task<string> UploadAsync(Stream content, string path, string? contentType = null, CancellationToken cancellationToken = default) => Task.FromResult(path);
        public Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(new MemoryStream(Encoding.UTF8.GetBytes("test")));
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(_exists);
    }
}
