namespace Edp.Organization.Application.Commands;

public sealed record CreateOrganizationCommand(string Name, string? Description = null);
