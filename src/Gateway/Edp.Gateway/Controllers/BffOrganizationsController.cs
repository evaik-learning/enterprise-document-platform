using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Edp.Gateway.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Controllers;

[ApiController]
[Authorize(Policy = Security.AuthorizationPolicies.GatewayAccess)]
[Route("bff/organizations")]
public sealed class BffOrganizationsController : ControllerBase
{
    private readonly IHttpClientFactory _clients;
    private readonly GatewayOptions _options;

    public BffOrganizationsController(IHttpClientFactory clients, IOptions<GatewayOptions> options)
    {
        _clients = clients;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var organizations = await RequestMemberships(cancellationToken);
        return organizations is null ? StatusCode(StatusCodes.Status503ServiceUnavailable) : Ok(organizations);
    }

    [HttpPost("{organizationId:guid}/select")]
    public async Task<IActionResult> Select(Guid organizationId, CancellationToken cancellationToken)
    {
        var organizations = await RequestMemberships(cancellationToken);
        if (organizations is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
        var membership = organizations.SingleOrDefault(x => x.Id == organizationId);
        if (membership is null || !membership.IsActive) return Forbid();

        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null) return Unauthorized();
        var existingIdentity = result.Principal.Identity as ClaimsIdentity;
        var identity = new ClaimsIdentity(result.Principal.Claims, CookieAuthenticationDefaults.AuthenticationScheme, existingIdentity?.NameClaimType, existingIdentity?.RoleClaimType);
        foreach (var claim in identity.FindAll("organization_id").ToArray()) identity.RemoveClaim(claim);
        foreach (var claim in identity.FindAll("edp_role").ToArray()) identity.RemoveClaim(claim);
        foreach (var claim in identity.FindAll("permission").ToArray()) identity.RemoveClaim(claim);
        identity.AddClaim(new Claim("organization_id", organizationId.ToString()));
        identity.AddClaim(new Claim("edp_role", membership.Role));
        identity.AddClaim(new Claim(ClaimTypes.Role, membership.Role));
        var capabilities = await RequestCapabilities(organizationId, cancellationToken);
        if (capabilities is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
        foreach (var permission in capabilities.Permissions)
            identity.AddClaim(new Claim("permission", permission));
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, result.Properties);
        return Ok(new { organizationId });
    }

    private async Task<List<OrganizationMembership>?> RequestMemberships(CancellationToken cancellationToken)
    {
        var destination = _options.ServiceCommunication.DownstreamServices.FirstOrDefault(x => x.Key.Equals("Organization", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(destination)) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{destination.TrimEnd('/')}/api/v1/Organizations/mine");
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", HttpContext.TraceIdentifier);
        using var response = await _clients.CreateClient("DownstreamServices").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<OrganizationMembership>>(cancellationToken);
    }

    private async Task<OrganizationCapabilities?> RequestCapabilities(Guid organizationId, CancellationToken cancellationToken)
    {
        var destination = _options.ServiceCommunication.DownstreamServices.FirstOrDefault(x => x.Key.Equals("Organization", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(destination)) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{destination.TrimEnd('/')}/api/v1/Organizations/{organizationId}/capabilities");
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", HttpContext.TraceIdentifier);
        using var response = await _clients.CreateClient("DownstreamServices").SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<OrganizationCapabilities>(cancellationToken)
            : null;
    }

    private sealed record OrganizationMembership(Guid Id, string Name, string? Description, string Role, bool IsActive);
    private sealed record OrganizationCapabilities(Guid OrganizationId, Guid UserId, string Role, IReadOnlyList<string> Permissions);
}
