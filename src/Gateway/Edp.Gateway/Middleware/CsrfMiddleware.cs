using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Edp.Gateway.Middleware;

/// <summary>Double-submit CSRF protection for cookie-authenticated browser commands.</summary>
public sealed class CsrfMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    private const string CookieName = "edp-csrf";
    private const string HeaderName = "X-CSRF-TOKEN";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey(CookieName))
        {
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            context.Response.Cookies.Append(CookieName, token, new CookieOptions
            {
                HttpOnly = false,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"
            });
        }

        var unsafeMethod = !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method)
            && !HttpMethods.IsOptions(context.Request.Method);
        var browserCommand = context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/bff");
        if (unsafeMethod && browserCommand && !Valid(context))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/400",
                title = "Invalid anti-forgery token",
                detail = "Refresh the page and retry the operation.",
                code = "CSRF_VALIDATION_FAILED",
                traceId = context.TraceIdentifier
            });
            return;
        }

        await next(context);
    }

    private static bool Valid(HttpContext context)
    {
        var cookie = context.Request.Cookies[CookieName];
        var header = context.Request.Headers[HeaderName].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(cookie)
            && !string.IsNullOrWhiteSpace(header)
            && CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(cookie),
                System.Text.Encoding.UTF8.GetBytes(header));
    }
}
