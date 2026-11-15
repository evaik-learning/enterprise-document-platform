using Edp.Gateway.Configuration;
using Edp.Gateway.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Controllers;

public sealed record AuthConfigResponse(bool DevAuthEnabled, bool EntraConfigured, IReadOnlyList<DevUserSummary> DevUsers);

public sealed record DevUserSummary(Guid Id, string DisplayName, string Email, string Description);

/// <summary>Sign-in options for the login page, plus the local-only demo sign-in (never active outside Development).</summary>
[ApiController]
[Route("bff/auth")]
public sealed class DevAuthController(
    IOptions<GatewayOptions> options,
    IWebHostEnvironment environment,
    DevTokenService tokens,
    IAuthenticationSchemeProvider schemes) : ControllerBase
{
    private bool DevAuthActive => options.Value.DevAuth.Enabled && environment.IsDevelopment();

    [HttpGet("config")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthConfigResponse>> GetConfig()
    {
        var entra = await schemes.GetSchemeAsync("OpenIdConnect") is not null;
        var users = DevAuthActive ? tokens.Users.Select(user => new DevUserSummary(user.Id, user.DisplayName, user.Email, user.Description)).ToArray() : [];
        return Ok(new AuthConfigResponse(DevAuthActive, entra, users));
    }

    [HttpGet("dev-login")]
    [AllowAnonymous]
    public async Task<IActionResult> DevLogin([FromQuery] Guid userId, [FromQuery] string? returnUrl)
    {
        if (!DevAuthActive) return NotFound();
        var user = tokens.Find(userId);
        if (user is null) return NotFound();

        var (token, expiresAt) = tokens.Issue(user);
        var properties = new AuthenticationProperties { IsPersistent = false, ExpiresUtc = expiresAt };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = token }]);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, tokens.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme), properties);

        // Relative redirects only: keeps the browser on the origin it signed in from.
        var target = returnUrl is not null && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\") ? returnUrl : "/";
        return LocalRedirect(target);
    }
}
