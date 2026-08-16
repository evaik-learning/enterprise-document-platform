using System.Security.Claims;

namespace Edp.Shared.Security.CurrentUser;

public interface ICurrentOrganization
{
    Guid? OrganizationId { get; }
    bool IsInOrganization { get; }
}

public sealed class CurrentOrganization : ICurrentOrganization
{
    public Guid? OrganizationId { get; init; }
    public bool IsInOrganization => OrganizationId.HasValue;

    public static CurrentOrganization FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            return new CurrentOrganization { OrganizationId = null };
        }

        var organizationIdClaim = principal.FindFirst("organization_id")
            ?? principal.FindFirst("org_id")
            ?? principal.FindFirst("tenant_id")
            ?? principal.FindFirst("tid");

        Guid? organizationId = null;
        if (organizationIdClaim is not null && Guid.TryParse(organizationIdClaim.Value, out var parsedOrganizationId))
        {
            organizationId = parsedOrganizationId;
        }

        return new CurrentOrganization { OrganizationId = organizationId };
    }
}
