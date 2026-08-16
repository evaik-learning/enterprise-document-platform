using Edp.Template.Domain.Enums;

namespace Edp.Template.Domain.Entities;

/// <summary>A dynamic value expected by a specific template version at generation time.</summary>
public sealed class Placeholder
{
    public Guid Id { get; init; }
    public Guid TemplateVersionId { get; init; }
    public string Name { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public PlaceholderDataType DataType { get; private set; } = PlaceholderDataType.String;
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
    public string? Format { get; private set; }
    public string? Description { get; private set; }
    public int Occurrences { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public static Placeholder Create(
        Guid templateVersionId,
        string name,
        int occurrences,
        PlaceholderDataType dataType = PlaceholderDataType.String,
        bool isRequired = true,
        string? displayName = null,
        string? defaultValue = null,
        string? format = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Placeholder
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = templateVersionId,
            Name = name,
            DisplayName = displayName ?? name,
            DataType = dataType,
            IsRequired = isRequired,
            DefaultValue = defaultValue,
            Format = format,
            Description = description ?? $"Dynamic value for {displayName ?? name}.",
            Occurrences = occurrences
        };
    }

    public void UpdateMetadata(
        string? displayName,
        PlaceholderDataType dataType,
        bool isRequired,
        string? defaultValue,
        string? format,
        string? description)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Name : displayName.Trim();
        DataType = dataType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        Format = string.IsNullOrWhiteSpace(format) ? null : format.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
