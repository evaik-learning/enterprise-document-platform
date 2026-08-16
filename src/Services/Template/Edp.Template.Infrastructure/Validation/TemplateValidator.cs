using System.Text.RegularExpressions;
using Edp.Shared.Storage.Abstractions;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;

namespace Edp.Template.Infrastructure.Validation;

public sealed class TemplateValidator : ITemplateValidator
{
    private static readonly Regex PlaceholderNameRegex = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly ITemplateVersionRepository _versionRepo;
    private readonly IPlaceholderRepository _placeholderRepo;
    private readonly IBlobStorageService _storage;

    public TemplateValidator(ITemplateVersionRepository versionRepo, IPlaceholderRepository placeholderRepo, IBlobStorageService storage)
    {
        _versionRepo = versionRepo;
        _placeholderRepo = placeholderRepo;
        _storage = storage;
    }

    public async Task<ValidationResultDto> ValidateAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationIssueDto>();
        var warnings = new List<ValidationIssueDto>();

        var version = await _versionRepo.GetByIdAsync(organizationId, templateId, versionId, cancellationToken);
        if (version is null)
        {
            errors.Add(new ValidationIssueDto { Code = "TPL000", Severity = "Error", Message = "Template version not found." });
            return BuildResult(errors, warnings);
        }

        // Structural validation
        if (string.IsNullOrWhiteSpace(version.StoragePath) || !await _storage.ExistsAsync(version.StoragePath, cancellationToken))
        {
            errors.Add(new ValidationIssueDto { Code = "TPL001", Severity = "Error", Message = "Template file not found in storage." });
        }

        // Placeholder validation
        var placeholders = await _placeholderRepo.GetByVersionIdAsync(version.Id, cancellationToken);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var placeholder in placeholders)
        {
            if (!PlaceholderNameRegex.IsMatch(placeholder.Name))
            {
                errors.Add(new ValidationIssueDto
                {
                    Code = "TPL002",
                    Severity = "Error",
                    Message = $"Invalid placeholder syntax: {{{{{placeholder.Name}}}}}",
                    Location = placeholder.Name
                });
                continue;
            }

            if (!seenNames.Add(placeholder.Name))
            {
                warnings.Add(new ValidationIssueDto
                {
                    Code = "TPL101",
                    Severity = "Warning",
                    Message = $"Duplicate placeholder definition: {placeholder.Name}",
                    Location = placeholder.Name
                });
            }

            if (string.IsNullOrWhiteSpace(placeholder.Format) && placeholder.DataType is Domain.Enums.PlaceholderDataType.Date or Domain.Enums.PlaceholderDataType.DateTime)
            {
                warnings.Add(new ValidationIssueDto
                {
                    Code = "TPL102",
                    Severity = "Warning",
                    Message = $"{placeholder.Name} has no display format.",
                    Location = placeholder.Name
                });
            }
        }

        if (placeholders.Count == 0)
        {
            warnings.Add(new ValidationIssueDto { Code = "TPL103", Severity = "Warning", Message = "No placeholders were detected in this template version." });
        }

        return BuildResult(errors, warnings);
    }

    private static ValidationResultDto BuildResult(List<ValidationIssueDto> errors, List<ValidationIssueDto> warnings) => new()
    {
        IsValid = errors.Count == 0,
        ErrorCount = errors.Count,
        WarningCount = warnings.Count,
        Errors = errors,
        Warnings = warnings
    };
}
