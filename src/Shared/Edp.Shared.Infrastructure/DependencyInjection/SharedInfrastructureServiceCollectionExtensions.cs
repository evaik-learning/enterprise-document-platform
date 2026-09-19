using System.Security.Claims;
using System.Text;
using Azure.Storage.Blobs;
using Edp.Shared.Infrastructure.Cache;
using Edp.Shared.Infrastructure.Configuration;
using Edp.Shared.Infrastructure.Persistence;
using Edp.Shared.Security.CurrentUser;
using Edp.Shared.Storage;
using Edp.Shared.Storage.Abstractions;
using Edp.SharedKernel.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Edp.Shared.Infrastructure.DependencyInjection;

public static class SharedInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<ICacheService, InMemoryCacheService>();
        return services;
    }

    public static IServiceCollection AddCurrentUserContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser>(sp =>
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            var principal = accessor.HttpContext?.User ?? new ClaimsPrincipal();
            return CurrentUser.FromClaimsPrincipal(principal);
        });
        services.AddScoped<ICurrentOrganization>(sp =>
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            var principal = accessor.HttpContext?.User ?? new ClaimsPrincipal();
            var trustedOrganization = accessor.HttpContext?.Request.Headers["X-EDP-Organization"].FirstOrDefault();
            return CurrentOrganization.FromClaimsPrincipal(principal, trustedOrganization);
        });

        return services;
    }

    public static IServiceCollection AddSharedJwtBearerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            if (!string.IsNullOrWhiteSpace(jwtOptions.Authority))
            {
                options.Authority = jwtOptions.Authority;
                options.MetadataAddress = $"{jwtOptions.Authority.TrimEnd('/')}/.well-known/openid-configuration";
            }

            if (!string.IsNullOrWhiteSpace(jwtOptions.Audience))
            {
                options.Audience = jwtOptions.Audience;
            }

            if (!string.IsNullOrWhiteSpace(jwtOptions.Issuer))
            {
                options.TokenValidationParameters.ValidIssuer = jwtOptions.Issuer;
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOptions.Issuer),
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(jwtOptions.Audience),
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(jwtOptions.Key),
                NameClaimType = "name",
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.FromMinutes(2),
                RequireSignedTokens = !string.IsNullOrWhiteSpace(jwtOptions.Key) || !string.IsNullOrWhiteSpace(jwtOptions.Authority),
                ValidateActor = false
            };

            if (!string.IsNullOrWhiteSpace(jwtOptions.Key))
            {
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
            }

            options.MapInboundClaims = false;
        });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSharedAuthorization(this IServiceCollection services, params string[] policyNames)
    {
        services.AddAuthorization(options =>
        {
            foreach (var policyName in policyNames.Where(static name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                options.AddPolicy(policyName, policy => policy.RequireAuthenticatedUser());
            }
        });

        return services;
    }

    public static IServiceCollection AddSharedAuthorization(this IServiceCollection services, Action<AuthorizationOptions> configurePolicies)
    {
        services.AddAuthorization(configurePolicies);
        return services;
    }

    public static IServiceCollection AddUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork<TDbContext>>();
        return services;
    }

    public static IServiceCollection AddAzureBlobStorage(this IServiceCollection services, string connectionString, string containerName = "documents")
    {
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>(sp =>
            new AzureBlobStorageService(sp.GetRequiredService<BlobServiceClient>(), containerName));
        return services;
    }

    public static async Task ApplyEntityFrameworkMigrationsAsync<TDbContext>(this WebApplication app, CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
