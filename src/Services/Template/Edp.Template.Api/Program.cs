#pragma warning disable CA1050 // Generated ASP.NET Core Program type

using System.Diagnostics.Metrics;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Edp.Template.Api.Filters;
using Edp.Template.Api.Security;
using Edp.Template.Application;
using Edp.Template.Infrastructure;
using Edp.Template.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<TemplateExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddSharedInfrastructure();
builder.Services.AddCurrentUserContext();

builder.Services.AddTemplateAuthorization();

builder.Services.AddTemplateApplication();
builder.Services.AddTemplateInfrastructure(builder.Configuration);

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddHealthChecks()
    .AddCheck<TemplateDbHealthCheck>("template-db");

var serviceName = "Edp.Template.Api";
var meter = new Meter(serviceName);
var templateRequestsCounter = meter.CreateCounter<long>("edp.template.http.requests");
var templateRequestDuration = meter.CreateHistogram<double>("edp.template.http.duration_ms");

builder.Services.AddSingleton(meter);
builder.Services.AddSingleton(templateRequestsCounter);
builder.Services.AddSingleton(templateRequestDuration);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddMeter(serviceName);
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddSource(serviceName);
    });

var app = builder.Build();

app.UseSharedPlatformMiddleware();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var correlationId = context.Items["X-Correlation-ID"]?.ToString() ?? context.TraceIdentifier;
    using var activity = new System.Diagnostics.ActivitySource(serviceName).StartActivity("TemplateRequest");
    activity?.SetTag("correlation.id", correlationId);
    context.Items["template.request.startedAtUtc"] = DateTimeOffset.UtcNow;
    await next();
    var startedAt = context.Items["template.request.startedAtUtc"] as DateTimeOffset? ?? DateTimeOffset.UtcNow;
    var elapsedMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
    templateRequestsCounter.Add(1, new KeyValuePair<string, object?>("route", context.Request.Path.Value ?? "unknown"), new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));
    templateRequestDuration.Record(elapsedMs, new KeyValuePair<string, object?>("route", context.Request.Path.Value ?? "unknown"));
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Template API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();

public sealed class TemplateDbHealthCheck(TemplateDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Template database reachable")
                : HealthCheckResult.Unhealthy("Template database unavailable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Template database unavailable", ex);
        }
    }
}

public partial class Program;
