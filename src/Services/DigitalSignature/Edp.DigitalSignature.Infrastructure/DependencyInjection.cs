namespace Edp.DigitalSignature.Infrastructure;

using Edp.DigitalSignature.Application.Interfaces;
using Edp.DigitalSignature.Infrastructure.Persistence;
using Edp.DigitalSignature.Infrastructure.Providers;
using Edp.DigitalSignature.Infrastructure.Repositories;
using Edp.DigitalSignature.Infrastructure.Outbox;
using Edp.DigitalSignature.Infrastructure.Messaging;
using Edp.DigitalSignature.Infrastructure.Storage;
using Edp.DigitalSignature.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency Injection extension for Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Digital Signature infrastructure services to the DI container.
    /// </summary>
    public static IServiceCollection AddDigitalSignatureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<SigningDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("SigningDb") 
                ?? throw new InvalidOperationException("Connection string 'SigningDb' not found in configuration.");
            
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(DependencyInjection).Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                });
        });

        // Register Unit of Work (wrapper around DbContext)
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<SigningDbContext>();

        // Register repositories (they will be created via UnitOfWork in production)
        services.AddScoped<ISigningRequestRepository, SigningRequestRepository>();
        services.AddScoped<ISignerRepository, SignerRepository>();
        services.AddScoped<ISignatureFieldRepository, SignatureFieldRepository>();
        services.AddScoped<ISignatureActionRepository, SignatureActionRepository>();
        services.AddScoped<ISigningProviderTransactionRepository, SigningProviderTransactionRepository>();

        // Register signature providers
        services.AddScoped<LocalDemoSignatureProvider>();
        services.AddScoped<SignatureProviderFactory>();
        services.AddScoped<SigningOutboxRepository>();
        services.AddScoped<SignedDocumentStorage>();
        services.AddScoped<IDocumentContentStore>(provider => provider.GetRequiredService<SignedDocumentStorage>());
        services.AddScoped<ISignatureProviderResolver>(provider => provider.GetRequiredService<SignatureProviderFactory>());
        services.AddHostedService<OutboxBackgroundService>();
        services.AddHostedService<SigningExpirationWorker>();
        services.AddHostedService<SigningReminderWorker>();

        // Register ISignatureProvider as singleton using factory
        services.AddScoped<ISignatureProvider>(provider =>
        {
            var factory = provider.GetRequiredService<SignatureProviderFactory>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var providerName = configuration["DigitalSignature:Provider"] ?? "LocalDemo";
            return factory.CreateProvider(providerName);
        });

        return services;
    }

    /// <summary>
    /// Applies pending migrations and seeds initial data.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigningDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
