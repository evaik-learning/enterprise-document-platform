namespace Edp.Gateway.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public string ServiceName { get; init; } = "Edp.Gateway";

    public string PublicApiBasePath { get; init; } = "/api/v1";

    public CorrelationOptions Correlation { get; init; } = new();

    public RateLimitOptions RateLimiting { get; init; } = new();

    public SecurityHeaderOptions SecurityHeaders { get; init; } = new();

    public EntraIdOptions EntraId { get; init; } = new();

    public ServiceCommunicationOptions ServiceCommunication { get; init; } = new();
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
    public string ContentSecurityPolicy { get; init; } = "default-src 'self'; connect-src 'self'; frame-ancestors 'none';";

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
