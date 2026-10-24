using System.Text.Json;

namespace Edp.Gateway.Services;

/// <summary>Null-tolerant accessors for downstream JSON payloads (property names are matched case-insensitively).</summary>
public static class JsonElementExtensions
{
    public static JsonElement? Property(this JsonElement? element, string name) =>
        element is { ValueKind: JsonValueKind.Object } value ? value.Property(name) : null;

    public static JsonElement? Property(this JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }

        return null;
    }

    public static string? GetString(this JsonElement? element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    public static string? GetString(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    public static Guid? GetGuid(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.String } value && value.TryGetGuid(out var guid) ? guid : null;

    public static int? GetInt(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var number) ? number : null;

    public static int? GetInt(this JsonElement? element, string name) =>
        element is { } value ? value.GetInt(name) : null;

    public static bool? GetBool(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.True or JsonValueKind.False } value ? value.GetBoolean() : null;

    public static DateTimeOffset? GetDate(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.String } value && value.TryGetDateTimeOffset(out var date) ? date : null;

    /// <summary>Items of a bare array or of a paged envelope with an <c>items</c> array.</summary>
    public static IReadOnlyList<JsonElement> Items(this JsonElement? element)
    {
        if (element is not { } value) return [];
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().ToList();
        if (value.Property("items") is { ValueKind: JsonValueKind.Array } items) return items.EnumerateArray().ToList();
        return [];
    }

    /// <summary>Total of a paged envelope, falling back to the number of returned items.</summary>
    public static int TotalCount(this JsonElement? element) =>
        element.GetInt("totalCount") ?? element.Items().Count;
}
