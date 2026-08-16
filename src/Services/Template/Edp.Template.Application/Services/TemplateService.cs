using System.Security.Cryptography;
using System.Text.Json;
using Edp.Shared.Storage.Abstractions;
using Edp.Template.Application.Commands;
using Edp.Template.Application.Common;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;
using Edp.Template.Application.Exceptions;
using Edp.Template.Application.Interfaces;
using Edp.Template.Domain.Entities;
using Edp.Template.Domain.Enums;

namespace Edp.Template.Application.Services;

public sealed class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _templates;
    private readonly ITemplateVersionRepository _versions;
    private readonly IPlaceholderRepository _placeholders;
    private readonly IValidationResultRepository _validationResults;
    private readonly IBlobStorageService _storage;
    private readonly IPlaceholderExtractor _extractor;
    private readonly ITemplateValidator _validator;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ITemplateAuditLogger _auditLogger;
    private readonly TemplateUploadSettings _uploadSettings;

    public TemplateService(
        ITemplateRepository templates,
        ITemplateVersionRepository versions,
        IPlaceholderRepository placeholders,
        IValidationResultRepository validationResults,
        IBlobStorageService storage,
        IPlaceholderExtractor extractor,
        ITemplateValidator validator,
        IIntegrationEventPublisher eventPublisher,
        ITemplateAuditLogger auditLogger,
        TemplateUploadSettings uploadSettings)
    {
        _templates = templates;
        _versions = versions;
        _placeholders = placeholders;
        _validationResults = validationResults;
        _storage = storage;
        _extractor = extractor;
        _validator = validator;
        _eventPublisher = eventPublisher;
        _auditLogger = auditLogger;
        _uploadSettings = uploadSettings;
    }

    public async Task<TemplateDto> CreateAsync(Guid organizationId, Guid? userId, CreateTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = global::Edp.Template.Domain.Entities.Template.NormalizeCode(command.Code);
        if (await _templates.CodeExistsAsync(organizationId, normalizedCode, cancellationToken))
        {
            throw new TemplateCodeConflictException($"A template with code '{normalizedCode}' already exists in this organization.");
        }

        var template = global::Edp.Template.Domain.Entities.Template.Create(Guid.NewGuid(), organizationId, command.Name, normalizedCode, command.Description, userId);

        await _templates.AddAsync(template, cancellationToken);
        await PublishAndClearAsync(template, cancellationToken);
        await _auditLogger.RecordAsync(organizationId, userId, "Create", "Template", template.Id, new Dictionary<string, object?>
        {
            ["code"] = template.Code,
            ["name"] = template.Name
        }, cancellationToken);

        return ToDto(template);
    }

    public async Task<TemplateDto?> GetAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(organizationId, templateId, cancellationToken);
        return template is null ? null : ToDto(template);
    }

    public async Task<PagedResult<TemplateDto>> ListAsync(Guid organizationId, string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await _templates.SearchAsync(organizationId, search, status, page, pageSize, cancellationToken);

        return new PagedResult<TemplateDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TemplateDto> UpdateAsync(Guid organizationId, Guid? userId, Guid templateId, UpdateTemplateCommand command, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(organizationId, templateId, cancellationToken)
            ?? throw new TemplateNotFoundException($"Template '{templateId}' was not found.");

        if (template.RowVersion is not null && !template.RowVersion.SequenceEqual(command.RowVersion))
        {
            throw new TemplateConcurrencyConflictException("The template was modified by another user. Reload and try again.");
        }

        template.UpdateDetails(command.Name, command.Description, userId);

        await _templates.UpdateAsync(template, cancellationToken);
        await PublishAndClearAsync(template, cancellationToken);
        await _auditLogger.RecordAsync(organizationId, userId, "Update", "Template", template.Id, new Dictionary<string, object?>
        {
            ["name"] = command.Name,
            ["description"] = command.Description
        }, cancellationToken);

        return ToDto(template);
    }

    public async Task<TemplateVersionDto> UploadVersionAsync(
        Guid organizationId,
        Guid? userId,
        Guid templateId,
        Stream content,
        string fileName,
        string contentType,
        long fileSize,
        string? changeDescription,
        CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(organizationId, templateId, cancellationToken)
            ?? throw new TemplateNotFoundException($"Template '{templateId}' was not found.");

        if (template.Status == TemplateStatus.Archived)
        {
            throw new TemplateOperationNotAllowedException("Cannot upload a new version to an archived template.");
        }

        ValidateUploadedFile(fileName, contentType, fileSize);

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var fileHash = Convert.ToHexString(await SHA256.HashDataAsync(buffer, cancellationToken)).ToLowerInvariant();
        buffer.Position = 0;

        var versionNumber = await _versions.GetNextVersionNumberAsync(templateId, cancellationToken);
        var versionId = Guid.NewGuid();
        var storagePath = $"organizations/{organizationId}/templates/{templateId}/versions/{versionId}/{fileName}";

        buffer.Position = 0;
        await _storage.UploadAsync(buffer, storagePath, contentType, cancellationToken);

        buffer.Position = 0;
        var extracted = (await _extractor.ExtractAsync(buffer, cancellationToken)).ToList();

        var version = TemplateVersion.Create(
            versionId,
            templateId,
            organizationId,
            versionNumber,
            fileName,
            _uploadSettings.BlobContainer,
            storagePath,
            fileHash,
            fileSize,
            contentType,
            changeDescription,
            userId);

        await _versions.AddAsync(version, cancellationToken);

        await _auditLogger.RecordAsync(organizationId, userId, "Upload", "TemplateVersion", version.Id, new Dictionary<string, object?>
        {
            ["templateId"] = templateId,
            ["versionNumber"] = versionNumber,
            ["fileName"] = fileName,
            ["fileSize"] = fileSize,
            ["changeDescription"] = changeDescription
        }, cancellationToken);

        var placeholderEntities = extracted
            .Select(p =>
            {
                var dataType = Enum.TryParse<PlaceholderDataType>(p.DataType, true, out var parsedDataType)
                    ? parsedDataType
                    : PlaceholderDataType.String;

                return Placeholder.Create(
                    version.Id,
                    p.Name,
                    p.Occurrences,
                    dataType,
                    p.IsRequired,
                    p.DisplayName,
                    p.DefaultValue,
                    p.Format,
                    p.Description);
            })
            .ToList();

        if (placeholderEntities.Count > 0)
        {
            await _placeholders.AddRangeAsync(placeholderEntities, cancellationToken);
        }

        await PublishAndClearAsync(version, cancellationToken);

        return ToDto(version, placeholderEntities);
    }

    public async Task<IReadOnlyList<TemplateVersionDto>> GetVersionsAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
    {
        await EnsureTemplateExistsAsync(organizationId, templateId, cancellationToken);

        var versions = await _versions.GetByTemplateIdAsync(organizationId, templateId, cancellationToken);
        return versions.Select(v => ToDto(v, [])).ToList();
    }

    public async Task<TemplateVersionDto?> GetVersionAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken);
        if (version is null)
        {
            return null;
        }

        var placeholders = await _placeholders.GetByVersionIdAsync(versionId, cancellationToken);
        return ToDto(version, placeholders);
    }

    public async Task<(Stream Content, string ContentType, string FileName)?> DownloadVersionAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var stream = await _storage.DownloadAsync(version.StoragePath, cancellationToken);
        return stream is null ? null : (stream, version.ContentType, version.FileName);
    }

    public async Task<PlaceholderDto?> GetPlaceholderAsync(Guid organizationId, Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var placeholder = await _placeholders.GetByVersionAndIdAsync(versionId, placeholderId, cancellationToken);
        return placeholder is null ? null : ToDto(placeholder);
    }

    public async Task<PlaceholderDto> CreatePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CreatePlaceholderCommand command, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new TemplateOperationNotAllowedException("Placeholder name is required.");
        }

        var existing = await _placeholders.GetByVersionIdAsync(versionId, cancellationToken);
        if (existing.Any(p => string.Equals(p.Name, command.Name, StringComparison.Ordinal)))
        {
            throw new TemplateOperationNotAllowedException($"A placeholder named '{command.Name}' already exists for this version.");
        }

        var dataType = ParseDataType(command.DataType ?? nameof(PlaceholderDataType.String));
        var placeholder = Placeholder.Create(
            version.Id,
            command.Name.Trim(),
            command.Occurrences <= 0 ? 1 : command.Occurrences,
            dataType,
            command.IsRequired ?? true,
            command.DisplayName,
            command.DefaultValue,
            command.Format,
            command.Description);

        await _placeholders.AddAsync(placeholder, cancellationToken);
        return ToDto(placeholder);
    }

    public async Task<PlaceholderDto> UpdatePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, Guid placeholderId, UpdatePlaceholderCommand command, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var placeholder = await _placeholders.GetByVersionAndIdAsync(versionId, placeholderId, cancellationToken)
            ?? throw new TemplateNotFoundException($"Placeholder '{placeholderId}' was not found.");

        var parsedDataType = ParseDataType(command.DataType ?? placeholder.DataType.ToString());
        placeholder.UpdateMetadata(
            command.DisplayName,
            parsedDataType,
            command.IsRequired ?? placeholder.IsRequired,
            command.DefaultValue,
            command.Format,
            command.Description);

        await _placeholders.UpdateAsync(placeholder, cancellationToken);
        return ToDto(placeholder);
    }

    public async Task<bool> DeletePlaceholderAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var placeholder = await _placeholders.GetByVersionAndIdAsync(versionId, placeholderId, cancellationToken);
        if (placeholder is null)
        {
            return false;
        }

        await _placeholders.DeleteAsync(placeholder, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PlaceholderDto>> GetPlaceholdersAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        _ = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var placeholders = await _placeholders.GetByVersionIdAsync(versionId, cancellationToken);
        return placeholders.Select(ToDto).ToList();
    }

    public async Task<PlaceholderDiscoveryResultDto> DiscoverPlaceholdersAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var stream = await _storage.DownloadAsync(version.StoragePath, cancellationToken);
        if (stream is null)
        {
            throw new TemplateOperationNotAllowedException("The template file could not be loaded for placeholder discovery.");
        }

        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var discovered = (await _extractor.ExtractAsync(ms, cancellationToken)).ToList();
        var existing = (await _placeholders.GetByVersionIdAsync(versionId, cancellationToken)).ToList();

        var discoveredNames = discovered.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var existingNames = existing.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        var newPlaceholders = discoveredNames.Where(name => !existingNames.Contains(name)).OrderBy(name => name).ToList();
        var existingPlaceholders = discoveredNames.Where(name => existingNames.Contains(name)).OrderBy(name => name).ToList();
        var missingFromDocument = existingNames.Where(name => !discoveredNames.Contains(name)).OrderBy(name => name).ToList();

        var result = new PlaceholderDiscoveryResultDto
        {
            TemplateVersionId = versionId,
            Discovered = discoveredNames.OrderBy(name => name).ToList(),
            NewPlaceholders = newPlaceholders,
            ExistingPlaceholders = existingPlaceholders,
            MissingFromDocument = missingFromDocument,
            Status = missingFromDocument.Count == 0 && newPlaceholders.Count == 0 ? "Consistent" : "RequiresReview"
        };

        await _auditLogger.RecordAsync(organizationId, userId, "Discovery", "TemplateVersion", version.Id, new Dictionary<string, object?>
        {
            ["templateId"] = templateId,
            ["discoveredCount"] = result.Discovered.Count,
            ["missingFromDocument"] = missingFromDocument,
            ["newPlaceholders"] = newPlaceholders
        }, cancellationToken);

        return result;
    }

    public async Task<ValidationResultDto> ValidatePlaceholdersAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var stream = await _storage.DownloadAsync(version.StoragePath, cancellationToken);
        if (stream is null)
        {
            throw new TemplateOperationNotAllowedException("The template file could not be loaded for placeholder validation.");
        }

        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var discovered = (await _extractor.ExtractAsync(ms, cancellationToken)).ToList();
        var existing = (await _placeholders.GetByVersionIdAsync(versionId, cancellationToken)).ToList();

        var discoveredNames = discovered.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var existingNames = existing.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        var errors = new List<ValidationIssueDto>();
        var warnings = new List<ValidationIssueDto>();

        foreach (var name in existingNames.Where(name => !discoveredNames.Contains(name)))
        {
            errors.Add(new ValidationIssueDto
            {
                Code = "PLH201",
                Severity = "Error",
                Message = $"Definition exists but placeholder '{name}' is missing from the document.",
                Location = name
            });
        }

        foreach (var name in discoveredNames.Where(name => !existingNames.Contains(name)))
        {
            warnings.Add(new ValidationIssueDto
            {
                Code = "PLH202",
                Severity = "Warning",
                Message = $"Document contains placeholder '{name}' that has no definition.",
                Location = name
            });
        }

        if (existingNames.Count == 0 && discoveredNames.Count == 0)
        {
            warnings.Add(new ValidationIssueDto { Code = "PLH203", Severity = "Warning", Message = "No placeholders were detected in the document or definitions." });
        }

        var result = new ValidationResultDto
        {
            IsValid = errors.Count == 0,
            Status = errors.Count == 0 ? "Consistent" : "RequiresReview",
            ErrorCount = errors.Count,
            WarningCount = warnings.Count,
            Errors = errors,
            Warnings = warnings,
            ValidatedAt = DateTimeOffset.UtcNow
        };

        await _auditLogger.RecordAsync(organizationId, userId, "PlaceholderValidation", "TemplateVersion", version.Id, new Dictionary<string, object?>
        {
            ["templateId"] = templateId,
            ["errors"] = errors.Count,
            ["warnings"] = warnings.Count,
            ["status"] = result.Status
        }, cancellationToken);

        return result;
    }

    public async Task<ValidationResultDto> ValidateVersionAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var result = await _validator.ValidateAsync(organizationId, templateId, versionId, cancellationToken);

        var entity = ValidationResultEntity.Create(
            versionId,
            result.IsValid,
            result.ErrorCount,
            result.WarningCount,
            JsonSerializer.Serialize(result.Errors),
            JsonSerializer.Serialize(result.Warnings));

        await _validationResults.AddAsync(entity, cancellationToken);

        version.MarkValidated(result.IsValid);
        await _versions.UpdateAsync(version, cancellationToken);
        await PublishAndClearAsync(version, cancellationToken);
        await _auditLogger.RecordAsync(organizationId, userId, "Validation", "TemplateVersion", version.Id, new Dictionary<string, object?>
        {
            ["templateId"] = templateId,
            ["isValid"] = result.IsValid,
            ["errorCount"] = result.ErrorCount,
            ["warningCount"] = result.WarningCount
        }, cancellationToken);

        result.Status = entity.Status.ToString();
        result.ValidatedAt = entity.ValidatedAt;
        return result;
    }

    public async Task<ValidationResultDto?> GetValidationResultAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        _ = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        var entity = await _validationResults.GetLatestByVersionIdAsync(versionId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return new ValidationResultDto
        {
            IsValid = entity.Status == ValidationStatus.Valid,
            Status = entity.Status.ToString(),
            ErrorCount = entity.ErrorCount,
            WarningCount = entity.WarningCount,
            Errors = Deserialize(entity.ErrorsJson),
            Warnings = Deserialize(entity.WarningsJson),
            ValidatedAt = entity.ValidatedAt
        };
    }

    public async Task<TemplateDto> ActivateVersionAsync(Guid organizationId, Guid? userId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(organizationId, templateId, cancellationToken)
            ?? throw new TemplateNotFoundException($"Template '{templateId}' was not found.");

        var version = await _versions.GetByIdAsync(organizationId, templateId, versionId, cancellationToken)
            ?? throw new TemplateVersionNotFoundException($"Template version '{versionId}' was not found.");

        if (version.ValidationStatus != ValidationStatus.Valid)
        {
            throw new TemplateOperationNotAllowedException("Only a version that has passed validation may be activated.");
        }

        foreach (var active in await _versions.GetActiveVersionsAsync(templateId, cancellationToken))
        {
            active.Deactivate();
            await _versions.UpdateAsync(active, cancellationToken);
        }

        version.Activate();
        await _versions.UpdateAsync(version, cancellationToken);

        template.ActivateVersion(versionId, userId);
        await _templates.UpdateAsync(template, cancellationToken);

        await PublishAndClearAsync(template, cancellationToken);
        await _auditLogger.RecordAsync(organizationId, userId, "Activation", "Template", template.Id, new Dictionary<string, object?>
        {
            ["templateId"] = templateId,
            ["versionId"] = versionId,
            ["activatedBy"] = userId
        }, cancellationToken);

        return ToDto(template);
    }

    public async Task<TemplateDto> DeactivateAsync(Guid organizationId, Guid? userId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(organizationId, templateId, cancellationToken)
            ?? throw new TemplateNotFoundException($"Template '{templateId}' was not found.");

        template.Deactivate(userId);
        await _templates.UpdateAsync(template, cancellationToken);
        await PublishAndClearAsync(template, cancellationToken);
        await _auditLogger.RecordAsync(organizationId, userId, "Deactivation", "Template", template.Id, new Dictionary<string, object?>
        {
            ["templateId"] = templateId,
            ["deactivatedBy"] = userId
        }, cancellationToken);

        return ToDto(template);
    }

    public async Task<TemplateDto> ArchiveAsync(Guid organizationId, Guid? userId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(organizationId, templateId, cancellationToken)
            ?? throw new TemplateNotFoundException($"Template '{templateId}' was not found.");

        template.Archive(userId);
        await _templates.UpdateAsync(template, cancellationToken);
        await PublishAndClearAsync(template, cancellationToken);
        await _auditLogger.RecordAsync(organizationId, userId, "Archive", "Template", template.Id, new Dictionary<string, object?>
        {
            ["templateId"] = templateId,
            ["archivedBy"] = userId
        }, cancellationToken);

        return ToDto(template);
    }

    private async Task EnsureTemplateExistsAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken)
    {
        _ = await _templates.GetByIdAsync(organizationId, templateId, cancellationToken)
            ?? throw new TemplateNotFoundException($"Template '{templateId}' was not found.");
    }

    private void ValidateUploadedFile(string fileName, string contentType, long fileSize)
    {
        if (fileSize <= 0)
        {
            throw new TemplateFileValidationException("The uploaded file is empty.");
        }

        if (fileSize > _uploadSettings.MaxFileSizeBytes)
        {
            throw new TemplateFileValidationException($"The uploaded file exceeds the maximum allowed size of {_uploadSettings.MaxFileSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !_uploadSettings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new TemplateFileValidationException($"Unsupported file extension '{extension}'. Allowed extensions: {string.Join(", ", _uploadSettings.AllowedExtensions)}.");
        }

        if (!_uploadSettings.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new TemplateFileValidationException($"Unsupported content type '{contentType}'.");
        }
    }

    private static PlaceholderDataType ParseDataType(string typeName)
    {
        if (Enum.TryParse<PlaceholderDataType>(typeName, true, out var parsed))
        {
            return parsed;
        }

        return PlaceholderDataType.String;
    }

    private async Task PublishAndClearAsync(global::Edp.Template.Domain.Entities.Template template, CancellationToken cancellationToken)
    {
        await _eventPublisher.PublishRangeAsync(template.DomainEvents, cancellationToken);
        template.ClearDomainEvents();
    }

    private async Task PublishAndClearAsync(TemplateVersion version, CancellationToken cancellationToken)
    {
        await _eventPublisher.PublishRangeAsync(version.DomainEvents, cancellationToken);
        version.ClearDomainEvents();
    }

    private static List<Edp.Template.Application.Dto.ValidationIssueDto> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<Edp.Template.Application.Dto.ValidationIssueDto>>(json) ?? [];
    }

    private static TemplateDto ToDto(global::Edp.Template.Domain.Entities.Template template) => new()
    {
        Id = template.Id,
        OrganizationId = template.OrganizationId,
        Name = template.Name,
        Code = template.Code,
        Description = template.Description,
        Status = template.Status.ToString(),
        CurrentVersionId = template.CurrentVersionId,
        CreatedAt = template.CreatedAt,
        CreatedBy = template.CreatedBy,
        ModifiedAt = template.ModifiedAt,
        ModifiedBy = template.ModifiedBy,
        RowVersion = template.RowVersion is null ? string.Empty : Convert.ToBase64String(template.RowVersion)
    };

    private static TemplateVersionDto ToDto(TemplateVersion version, IReadOnlyList<Placeholder> placeholders) => new()
    {
        Id = version.Id,
        TemplateId = version.TemplateId,
        VersionNumber = version.VersionNumber,
        FileName = version.FileName,
        StoragePath = version.StoragePath,
        ContentType = version.ContentType,
        FileSize = version.FileSize,
        FileHash = version.FileHash,
        ValidationStatus = version.ValidationStatus.ToString(),
        Status = version.Status.ToString(),
        ChangeDescription = version.ChangeDescription,
        CreatedAt = version.CreatedAt,
        CreatedBy = version.CreatedBy,
        Placeholders = placeholders.Select(ToDto).ToList()
    };

    private static PlaceholderDto ToDto(Placeholder placeholder) => new()
    {
        Id = placeholder.Id,
        Name = placeholder.Name,
        DisplayName = placeholder.DisplayName,
        DataType = placeholder.DataType.ToString(),
        IsRequired = placeholder.IsRequired,
        DefaultValue = placeholder.DefaultValue,
        Format = placeholder.Format,
        Description = placeholder.Description,
        Occurrences = placeholder.Occurrences
    };
}
