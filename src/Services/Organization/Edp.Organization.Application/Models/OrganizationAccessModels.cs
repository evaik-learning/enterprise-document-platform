namespace Edp.Organization.Application.Models;

public static class OrganizationRoles
{
    public const string Owner = "Owner";
    public const string Administrator = "Administrator";
    public const string Member = "Member";
    public const string Auditor = "Auditor";

    public static bool IsAdministrator(string role) =>
        string.Equals(role, Owner, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Administrator, StringComparison.OrdinalIgnoreCase);
}

public sealed record OrganizationMembershipDto(
    Guid Id,
    string Name,
    string? Description,
    string Role,
    bool IsActive);

public sealed record OrganizationMemberDto(
    Guid UserId,
    string Role,
    bool IsActive);

public sealed record OrganizationCapabilityDto(
    Guid OrganizationId,
    Guid UserId,
    string Role,
    IReadOnlyList<string> Permissions);

public sealed record AddOrganizationMemberCommand(Guid UserId, string Role);
public sealed record UpdateOrganizationMemberRoleCommand(string Role);
