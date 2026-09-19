namespace Edp.DigitalSignature.Api;

using Edp.DigitalSignature.Application;
using Edp.DigitalSignature.Infrastructure;
using Edp.DigitalSignature.Infrastructure.Persistence;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Edp.Shared.Messaging;
using Edp.DigitalSignature.Api.Middleware;
using Edp.DigitalSignature.Application.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

/// <summary>
/// Digital Signature Service API
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddControllers();

        // Digital Signature Infrastructure (DbContext, repositories, services)
        services.AddDigitalSignatureInfrastructure(Configuration);

        // Application layer services
        services.AddDigitalSignatureApplication();

        // Shared infrastructure (ProblemDetailsExceptionMiddleware, CorrelationId, UnitOfWork)
        services.AddSharedInfrastructure();
        services.AddCurrentUserContext();
        services.AddSharedJwtBearerAuthentication(Configuration);
        services.AddSharedAuthorization(options =>
        {
            options.AddPolicy("signing.read", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("signing.write", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("SigningManager", policy => policy.RequireRole("SigningManager", "OrganizationAdmin"));
            options.AddPolicy("Signer", policy => policy.RequireRole("Signer", "SigningManager", "OrganizationAdmin"));
            options.AddPolicy("Auditor", policy => policy.RequireRole("Auditor", "SigningManager", "OrganizationAdmin"));
        });
        services.AddSharedServiceBusPublisher(Configuration, "ServiceBus:SigningTopic", "signing-events");

        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(SigningTelemetry.ActivitySource.Name))
            .WithMetrics(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddMeter(SigningTelemetry.Meter.Name));

        services.AddAzureBlobStorage(
            Configuration.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true",
            "signed-documents");

        // Health checks
        services.AddHealthChecks();

        // OpenAPI documentation
        services.AddOpenApi();

        // Logging
        services.AddLogging(options =>
        {
            options.AddConsole();
            options.AddDebug();
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
    {
        app.UseMiddleware<SigningDomainExceptionMiddleware>();

        // Exception handling and correlation
        app.UseSharedPlatformMiddleware();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            // Health check endpoints
            endpoints.MapHealthChecks("/health/live");

            endpoints.MapHealthChecks("/health/ready");

            // OpenAPI documentation
            if (env.IsDevelopment())
            {
                endpoints.MapOpenApi();
            }
        });

        // Apply database migrations on startup
        using (var scope = serviceProvider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            try
            {
                sp.ApplyMigrationsAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILogger<Startup>>();
                logger.LogError(ex, "An error occurred while migrating the database");
                throw;
            }
        }
    }
}
