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
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
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
    public IActionResult Logout([FromQuery] string? returnUrl = null)
    {
        var finalReturnUrl = ResolveReturnUrl(returnUrl);

        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = finalReturnUrl
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
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
