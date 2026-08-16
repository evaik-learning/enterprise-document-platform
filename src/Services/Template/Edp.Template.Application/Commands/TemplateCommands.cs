namespace Edp.Template.Application.Commands;

public sealed record CreateTemplateCommand(string Name, string Code, string? Description);

public sealed record UpdateTemplateCommand(string Name, string? Description, byte[] RowVersion);

public sealed record CreatePlaceholderCommand(
    string Name,
    string? DisplayName,
    string? DataType,
    bool? IsRequired,
    string? DefaultValue,
    string? Format,
    string? Description,
    int Occurrences = 1);

public sealed record UpdatePlaceholderCommand(
    string? DisplayName,
    string? DataType,
    bool? IsRequired,
    string? DefaultValue,
    string? Format,
    string? Description);
