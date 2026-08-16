using System.Security.Claims;
using Azure.Storage.Blobs;
using Edp.Shared.Infrastructure.Cache;
using Edp.Shared.Infrastructure.Persistence;
using Edp.Shared.Security.CurrentUser;
using Edp.Shared.Storage;
using Edp.Shared.Storage.Abstractions;
using Edp.SharedKernel.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            return CurrentOrganization.FromClaimsPrincipal(principal);
        });

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
}
