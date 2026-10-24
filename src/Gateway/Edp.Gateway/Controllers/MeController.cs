using System.Security.Claims;
using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Controllers;

public sealed record MeResponse(
    string? UserId,
    string? Name,
    string? Email,
    string? OrganizationId,
    string? OrganizationName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool RequiresOrganizationSelection);

/// <summary>Current user, tenant and permission context for the web application.</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.GatewayAccess)]
[Route("api/v1/me")]
public sealed class MeController(IDownstreamApiClient downstream) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        var organizationId = User.FindFirst("organization_id")?.Value;
        string? organizationName = null;
        if (organizationId is not null)
        {
            var organization = await downstream.GetAsync(DownstreamServices.Organization, $"/api/v1/Organizations/{organizationId}", cancellationToken);
            if (organization.IsSuccess) organizationName = organization.Body.GetString("name");
        }

        var roles = User.FindAll("edp_role").Select(claim => claim.Value).Distinct().ToArray();
        var permissions = PermissionMapper.ToFrontend(User.FindAll("permission").Select(claim => claim.Value));
        var userId = (User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("oid") ?? User.FindFirst("sub"))?.Value;
        var name = User.FindFirst("name")?.Value ?? User.Identity?.Name;
        var email = User.FindFirst("email")?.Value ?? User.FindFirst("preferred_username")?.Value;

        return Ok(new MeResponse(userId, name, email, organizationId, organizationName, roles, permissions, organizationId is null));
    }
}
