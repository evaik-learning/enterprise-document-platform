using Edp.Template.Application.Commands;
using Edp.Template.Application.Dto;

namespace Edp.Template.Application.Interfaces;

public interface ITemplateService
{
    Task<TemplateDto> CreateAsync(Guid organizationId, Guid? userId, CreateTemplateCommand command, CancellationToken cancellationToken = default);

    Task<TemplateDto?> GetAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default);

    Task<PagedResult<TemplateDto>> ListAsync(Guid organizationId, string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<TemplateDto> UpdateAsync(Guid organizationId, Guid? userId, Guid templateId, UpdateTemplateCommand command, CancellationToken cancellationToken = default);

    Task<TemplateVersionDto> UploadVersionAsync(
        Guid organizationId,
        Guid? userId,
        Guid templateId,
        Stream content,
        string fileName,
        string contentType,
        long fileSize,
        string? changeDescription,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateVersionDto>> GetVersionsAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default);

    Task<TemplateVersionDto?> GetVersionAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<(Stream Content, string ContentType, string FileName)?> DownloadVersionAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<PlaceholderDto?> GetPlaceholderAsync(Guid organizationId, Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default);

    Task<PlaceholderDto> CreatePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CreatePlaceholderCommand command, CancellationToken cancellationToken = default);

    Task<PlaceholderDto> UpdatePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, Guid placeholderId, UpdatePlaceholderCommand command, CancellationToken cancellationToken = default);

    Task<bool> DeletePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaceholderDto>> GetPlaceholdersAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<PlaceholderDiscoveryResultDto> DiscoverPlaceholdersAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<ValidationResultDto> ValidatePlaceholdersAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<ValidationResultDto> ValidateVersionAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<ValidationResultDto?> GetValidationResultAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<TemplateDto> ActivateVersionAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<TemplateDto> DeactivateAsync(Guid organizationId, Guid? userId, Guid templateId, CancellationToken cancellationToken = default);

    Task<TemplateDto> ArchiveAsync(Guid organizationId, Guid? userId, Guid templateId, CancellationToken cancellationToken = default);
}
