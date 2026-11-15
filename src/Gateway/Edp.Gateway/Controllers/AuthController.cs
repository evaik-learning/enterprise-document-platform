using Edp.Gateway.Configuration;
using Edp.Gateway.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Controllers;

[ApiController]
[Route("bff/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly string _frontendBaseUrl;

    public AuthController(IOptions<GatewayOptions> gatewayOptions)
    {
        _frontendBaseUrl = gatewayOptions.Value.FrontendBaseUrl.TrimEnd('/');
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl = null, [FromServices] IAuthenticationSchemeProvider? schemes = null)
    {
        // Without Entra ID configured (local demo mode) the SPA login page offers demo users instead.
        if (schemes is not null && await schemes.GetSchemeAsync(OpenIdConnectDefaults.AuthenticationScheme) is null)
            return Redirect($"{_frontendBaseUrl}/login");

        var finalReturnUrl = ResolveReturnUrl(returnUrl);

        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri = finalReturnUrl
            },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromQuery] string? returnUrl = null, [FromServices] IAuthenticationSchemeProvider? schemes = null)
    {
        var finalReturnUrl = ResolveReturnUrl(returnUrl);
        var properties = new AuthenticationProperties { RedirectUri = finalReturnUrl };

        // Local demo sign-in has no identity-provider session to end.
        if (schemes is not null && await schemes.GetSchemeAsync(OpenIdConnectDefaults.AuthenticationScheme) is null)
            return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("user")]
    [AllowAnonymous]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new CurrentUserResponse(false, null, null, []));
        }

        var claims = User.Claims
            .Select(claim => new UserClaimResponse(claim.Type, claim.Value))
            .ToArray();

        var displayName = User.FindFirst("name")?.Value
            ?? User.Identity?.Name
            ?? User.FindFirst("preferred_username")?.Value;

        var userName = User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name;

        return Ok(new CurrentUserResponse(true, displayName, userName, claims));
    }

    [HttpGet("access-denied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return Forbid();
    }

    private string ResolveReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return _frontendBaseUrl;
        }

        if (Uri.TryCreate(_frontendBaseUrl, UriKind.Absolute, out var frontendUri)
            && Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps)
            && Uri.Compare(absoluteUri, frontendUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
        {
            return absoluteUri.PathAndQuery + absoluteUri.Fragment;
        }

        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        {
            return $"{_frontendBaseUrl}/{returnUrl.TrimStart('/')}";
        }

        return _frontendBaseUrl;
    }
}
