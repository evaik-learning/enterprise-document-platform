namespace Edp.Gateway.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public string ServiceName { get; init; } = "Edp.Gateway";

    public string PublicApiBasePath { get; init; } = "/api/v1";

    public string FrontendBaseUrl { get; init; } = "http://localhost:5173";

    /// <summary>Public origin users reach the app on (the web host that proxies to this Gateway). Used for OIDC redirect URIs.</summary>
    public string PublicBaseUrl { get; init; } = string.Empty;

    public CorrelationOptions Correlation { get; init; } = new();

    public RateLimitOptions RateLimiting { get; init; } = new();

    public SecurityHeaderOptions SecurityHeaders { get; init; } = new();

    public EntraIdOptions EntraId { get; init; } = new();

    public ServiceCommunicationOptions ServiceCommunication { get; init; } = new();

    public DevAuthOptions DevAuth { get; init; } = new();
}

/// <summary>Local-demo sign-in that replaces Entra ID. Refused outside the Development environment.</summary>
public sealed class DevAuthOptions
{
    public bool Enabled { get; init; }

    /// <summary>HS256 key shared with the backend services (Jwt:Key). At least 32 characters.</summary>
    public string SigningKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = "edp-local";

    public string Audience { get; init; } = "edp-api";

    public int TokenLifetimeMinutes { get; init; } = 480;

    public List<DevUserOptions> Users { get; init; } = [];
}

public sealed class DevUserOptions
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>Role/permission values placed in the token's <c>roles</c> claim, as Entra app roles would be.</summary>
    public string[] Roles { get; init; } = [];
}

public sealed class CorrelationOptions
{
    public string HeaderName { get; init; } = "X-Correlation-ID";
}

public sealed class RateLimitOptions
{
    public int PermitLimit { get; init; } = 100;

    public int WindowSeconds { get; init; } = 60;

    public int QueueLimit { get; init; } = 0;
}

public sealed class SecurityHeaderOptions
{
    public string ContentSecurityPolicy { get; init; } = "default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; img-src 'self' data:; font-src 'self' data:; connect-src 'self';";

    public string DevelopmentContentSecurityPolicy { get; init; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' http: https: ws: wss: localhost:* 127.0.0.1:*; frame-ancestors 'none';";

    public string ReferrerPolicy { get; init; } = "no-referrer";

    public string PermissionsPolicy { get; init; } = "geolocation=(), microphone=(), camera=()";
}

public sealed class EntraIdOptions
{
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    public string TenantId { get; init; } = "common";

    public string ClientId { get; init; } = "";

    public string ClientSecret { get; init; } = "";

    public string CallbackPath { get; init; } = "/signin-oidc";

    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";

    public string[] Scopes { get; init; } = ["openid", "profile", "email"];
}

public sealed class ServiceCommunicationOptions
{
    public int TimeoutSeconds { get; init; } = 30;

    public Dictionary<string, string> DownstreamServices { get; init; } = new();
}
