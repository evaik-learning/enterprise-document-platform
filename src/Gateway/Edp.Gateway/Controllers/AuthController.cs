using Edp.Gateway.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Controllers;

[ApiController]
[Route("bff/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        var targetUrl = "http://localhost:5173";
        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri = NormalizeLocalReturnUrl(targetUrl)
            },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    [Authorize]
    public IActionResult Logout([FromQuery] string? returnUrl = "/")
    {
        var targetUrl = returnUrl ?? "http://localhost:5173";
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = NormalizeLocalReturnUrl(targetUrl)
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

    private static string NormalizeLocalReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
            ? returnUrl
            : "/";
    }
}
