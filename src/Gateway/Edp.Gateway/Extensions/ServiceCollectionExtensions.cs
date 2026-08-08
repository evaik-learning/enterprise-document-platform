using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Edp.Gateway.Configuration;
using Edp.Gateway.Security;
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

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
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
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = $"{entraIdOptions.Instance.TrimEnd('/')}/{entraIdOptions.TenantId}/v2.0";
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

                options.Scope.Clear();
                foreach (var scope in entraIdOptions.Scopes)
                {
                    options.Scope.Add(scope);
                }

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenResponseReceived = context =>
                    {
                        // Access raw tokens here
                        var idToken = context.TokenEndpointResponse.IdToken;
                        var accessToken = context.TokenEndpointResponse.AccessToken;
                        var refreshToken = context.TokenEndpointResponse.RefreshToken;

                        // Log them for debugging (never in production!)
                        Console.WriteLine("ID Token: " + idToken);
                        Console.WriteLine("Access Token: " + accessToken);
                        Console.WriteLine("Refresh Token: " + refreshToken);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Inspect claims
                        foreach (var claim in context.Principal.Claims)
                        {
                            Console.WriteLine($"{claim.Type}: {claim.Value}");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

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
        var gatewayOptions = new GatewayOptions();
        configuration.GetSection(GatewayOptions.SectionName).Bind(gatewayOptions);

        services.AddRateLimiter(options =>
        {
            var rateLimitOptions = gatewayOptions.RateLimiting;

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

        return services;
    }

    public static IServiceCollection AddGatewayObservability(this IServiceCollection services)
    {
        services.AddSingleton(new ActivitySource(ObservabilityConstants.ActivitySourceName));
        services.AddSingleton(new Meter(ObservabilityConstants.MeterName));

        return services;
    }
}
