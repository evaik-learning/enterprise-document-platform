using Edp.Document.Application.Interfaces;
using Edp.Document.Contracts.Responses;

namespace Edp.Document.Infrastructure.Validation;

public sealed class PlaceholderValidator : IPlaceholderValidator
{
    public ValidationResponse Validate(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationIssueResponse>();
        foreach (var pair in data)
        {
            if (pair.Value is null)
            {
                errors.Add(new ValidationIssueResponse { Placeholder = pair.Key, Message = $"A value is required for '{pair.Key}'." });
            }
        }

        return new ValidationResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
