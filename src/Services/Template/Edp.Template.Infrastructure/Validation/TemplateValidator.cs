using System.Text.RegularExpressions;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;
using Edp.Template.Domain.Entities;

namespace Edp.Template.Infrastructure.Validation;

public class TemplateValidator : ITemplateValidator
{
    private static readonly Regex PlaceholderNameRegex = new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private readonly ITemplateVersionRepository _versionRepo;
    private readonly IPlaceholderRepository _placeholderRepo;
    private readonly ITemplateStorage _storage;

    public TemplateValidator(ITemplateVersionRepository versionRepo, IPlaceholderRepository placeholderRepo, ITemplateStorage storage)
    {
        _versionRepo = versionRepo;
        _placeholderRepo = placeholderRepo;
        _storage = storage;
    }

    public async Task<ValidationResultDto> ValidateAsync(Guid templateId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _versionRepo.GetByIdAsync(versionId, cancellationToken);
        if (version is null) return new ValidationResultDto { IsValid = false, ErrorCount = 1, Errors = new[] { "Version not found" } };

        var errors = new List<string>();
        var warnings = new List<string>();

        // Structural validation: blob exists
        if (string.IsNullOrWhiteSpace(version.StoragePath) || !await _storage.ExistsAsync(version.StoragePath, cancellationToken))
        {
            errors.Add("Template file not found in storage.");
        }

        // Placeholder validation
        var placeholders = await _placeholderRepo.GetByVersionIdAsync(version.Id, cancellationToken);
        foreach (var ph in placeholders)
        {
            if (!PlaceholderNameRegex.IsMatch(ph.Name))
            {
                errors.Add($"Invalid placeholder name: {ph.Name}");
            }
        }

        var result = new ValidationResultDto
        {
            IsValid = errors.Count == 0,
            ErrorCount = errors.Count,
            WarningCount = warnings.Count,
            Errors = errors,
            Warnings = warnings
        };

        return result;
    }
}
