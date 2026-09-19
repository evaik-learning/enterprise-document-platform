namespace Edp.DigitalSignature.Application;

using Microsoft.Extensions.DependencyInjection;
using Edp.DigitalSignature.Application.Interfaces;
using Edp.DigitalSignature.Application.Services;

/// <summary>
/// Application layer dependency injection extensions.
/// </summary>
public static class ApplicationLayer
{
    /// <summary>
    /// Adds application layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDigitalSignatureApplication(this IServiceCollection services)
    {
        services.AddScoped<ISigningRequestService, SigningRequestService>();
        services.AddScoped<ISigningAuditService, SigningAuditService>();

        return services;
    }
}
