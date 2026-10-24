using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Edp.Gateway.Configuration;
using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.OpenApi;

namespace Edp.Gateway.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GatewayOptions>(configuration.GetSection(GatewayOptions.SectionName));
        return services;
    }

    public static IServiceCollection AddGatewayApi(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new()
                {
                    Title = "EDP Gateway API",
                    Version = "v1",
                    Description = "Enterprise Document Platform Gateway"
                };

                return Task.CompletedTask;
            });
        });
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddGatewaySecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var gatewayOptions = new GatewayOptions();
        configuration.GetSection(GatewayOptions.SectionName).Bind(gatewayOptions);
        var entraIdOptions = gatewayOptions.EntraId;
        var hasValidOidcConfig = !string.IsNullOrWhiteSpace(entraIdOptions.TenantId)
            && !string.IsNullOrWhiteSpace(entraIdOptions.ClientId)
            && !string.IsNullOrWhiteSpace(entraIdOptions.ClientSecret);

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = hasValidOidcConfig ? OpenIdConnectDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-EdpGatewayAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/bff/auth/login";
                options.LogoutPath = "/bff/auth/logout";
                options.AccessDeniedPath = "/bff/auth/access-denied";
                options.Events.OnRedirectToLogin = context => RejectApiRequest(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => RejectApiRequest(context, StatusCodes.Status403Forbidden);
            });

        if (hasValidOidcConfig)
        {
            authenticationBuilder.AddOpenIdConnect(options =>
            {
                var instance = entraIdOptions.Instance ?? "https://login.microsoftonline.com/";
                options.Authority = $"{instance.TrimEnd('/')}/{entraIdOptions.TenantId}/v2.0";
                options.ClientId = entraIdOptions.ClientId;
                options.ClientSecret = entraIdOptions.ClientSecret;
                options.CallbackPath = entraIdOptions.CallbackPath;
                options.SignedOutCallbackPath = entraIdOptions.SignedOutCallbackPath;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    if (IsApiRequest(context.Request))
                    {
                        // XHR callers need a 401 they can act on, not a redirect to the identity provider.
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse();
                    }

                    return Task.CompletedTask;
                };

                var scopes = entraIdOptions.Scopes ?? Array.Empty<string>();
                options.Scope.Clear();
                foreach (var scope in scopes)
                {
                    if (!string.IsNullOrWhiteSpace(scope))
                    {
                        options.Scope.Add(scope);
                    }
                }
            });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.GatewayAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var gatewayOptions = configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>() ?? new GatewayOptions();

        services.AddRateLimiter(options =>
        {
            var rateLimitOptions = gatewayOptions.RateLimiting ?? new RateLimitOptions();

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds),
                        QueueLimit = rateLimitOptions.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        return services;
    }

    public static IServiceCollection AddGatewayServiceClients(this IServiceCollection services)
    {
        services.AddHttpClient("DownstreamServices", (serviceProvider, client) =>
        {
            var gatewayOptions = serviceProvider.GetRequiredService<IOptions<GatewayOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(gatewayOptions.ServiceCommunication.TimeoutSeconds);
        });

        services.AddScoped<IDownstreamApiClient, DownstreamApiClient>();
        services.AddScoped<WorkflowContextLoader>();

        return services;
    }

    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        var frontend = configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>()?.FrontendBaseUrl;
        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
        {
            if (Uri.TryCreate(frontend, UriKind.Absolute, out var origin))
            {
                policy.WithOrigins(origin.GetLeftPart(UriPartial.Authority))
                    .AllowCredentials()
                    .AllowAnyMethod()
                    .WithHeaders("Content-Type", "Accept", "X-Requested-With", "X-CSRF-TOKEN", "Idempotency-Key", "X-Correlation-ID")
                    .WithExposedHeaders("X-Correlation-ID", "Content-Disposition");
            }
        }));
        return services;
    }

    public const string CorsPolicyName = "Frontend";

    private static bool IsApiRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/api")
        || (request.Path.StartsWithSegments("/bff") && !request.Path.StartsWithSegments("/bff/auth"));

    private static Task RejectApiRequest(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        if (IsApiRequest(context.Request)) context.Response.StatusCode = statusCode;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    public static IServiceCollection AddGatewayObservability(this IServiceCollection services)
    {
        services.AddSingleton(new ActivitySource(ObservabilityConstants.ActivitySourceName));
        services.AddSingleton(new Meter(ObservabilityConstants.MeterName));

        return services;
    }
}
