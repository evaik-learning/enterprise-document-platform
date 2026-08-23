using Edp.Document.Application.Interfaces;
using Edp.Document.Contracts.Responses;

namespace Edp.Document.Application.Services;

public sealed class PlaceholderResolutionService : IPlaceholderResolutionService
{
    public Dictionary<string, object?> Resolve(string templateName, Dictionary<string, object?> data)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in data)
        {
            values[pair.Key] = pair.Value is string s ? s.Trim() : pair.Value;
        }

        if (!string.IsNullOrWhiteSpace(templateName))
        {
            values["templateName"] = templateName.Trim();
        }

        return values;
    }

    public ValidationResponse Validate(string templateName, Dictionary<string, object?> data)
    {
        var errors = new List<ValidationIssueResponse>();

        if (string.IsNullOrWhiteSpace(templateName))
        {
            errors.Add(new ValidationIssueResponse { Placeholder = "templateName", Message = "A template name is required." });
        }

        foreach (var pair in data)
        {
            if (pair.Value is null)
            {
                errors.Add(new ValidationIssueResponse { Placeholder = pair.Key, Message = $"Value for '{pair.Key}' is required." });
                continue;
            }

            if (pair.Value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                errors.Add(new ValidationIssueResponse { Placeholder = pair.Key, Message = $"Value for '{pair.Key}' is required." });
            }
        }

        return new ValidationResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
