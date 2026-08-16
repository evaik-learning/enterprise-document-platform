using System.Text;
using System.Text.RegularExpressions;
using Edp.Template.Domain.Enums;

namespace Edp.Template.Infrastructure.Document;

public sealed class PlaceholderEngine
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{(?<name>[A-Za-z][A-Za-z0-9_]*)\}\}", RegexOptions.Compiled);

    public static IReadOnlyList<PlaceholderDefinition> Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in PlaceholderRegex.Matches(text))
        {
            var name = match.Groups["name"].Value;
            counts[name] = counts.TryGetValue(name, out var current) ? current + 1 : 1;
        }

        return counts
            .Select(kvp => BuildDefinition(kvp.Key, kvp.Value))
            .ToArray();
    }

    private static PlaceholderDefinition BuildDefinition(string name, int occurrences)
    {
        var displayName = ToDisplayName(name);
        var dataType = InferDataType(name);

        return new PlaceholderDefinition
        {
            Name = name,
            DisplayName = displayName,
            DataType = dataType,
            IsRequired = true,
            Description = $"Dynamic value for {displayName}.",
            Occurrences = occurrences
        };
    }

    private static string ToDisplayName(string name)
    {
        var spaced = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1 $2");
        spaced = spaced.Replace('_', ' ');

        var words = spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(WordToTitleCase)
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(' ', words);
    }

    private static string WordToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length == 1)
        {
            return char.ToUpperInvariant(value[0]).ToString();
        }

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    private static PlaceholderDataType InferDataType(string name)
    {
        var normalized = name.ToLowerInvariant();

        if (normalized.Contains("email")) return PlaceholderDataType.Email;
        if (normalized.Contains("phone") || normalized.Contains("mobile")) return PlaceholderDataType.Phone;
        if (normalized.Contains("url") || normalized.Contains("link") || normalized.Contains("website")) return PlaceholderDataType.Url;
        if (normalized.Contains("amount") || normalized.Contains("total") || normalized.Contains("price") || normalized.Contains("cost") || normalized.Contains("balance")) return PlaceholderDataType.Decimal;
        if (normalized.Contains("date") || normalized.Contains("day") || normalized.Contains("month") || normalized.Contains("year")) return PlaceholderDataType.Date;
        if (normalized.Contains("datetime") || normalized.Contains("time")) return PlaceholderDataType.DateTime;
        if (normalized.Contains("is") || normalized.Contains("has") || normalized.Contains("enabled") || normalized.Contains("active") || normalized.Contains("approved")) return PlaceholderDataType.Boolean;
        if (normalized.Contains("count") || normalized.Contains("number") || normalized.Contains("id") || normalized.Contains("qty") || normalized.Contains("quantity")) return PlaceholderDataType.Integer;

        return PlaceholderDataType.String;
    }
}

public sealed class PlaceholderDefinition
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public PlaceholderDataType DataType { get; init; } = PlaceholderDataType.String;
    public bool IsRequired { get; init; } = true;
    public string? Description { get; init; }
    public int Occurrences { get; init; }
}
