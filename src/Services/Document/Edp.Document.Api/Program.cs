using System.Diagnostics.Metrics;
using Edp.Document.Application;
using Edp.Document.Infrastructure;
using Edp.Document.Infrastructure.Persistence;
using Edp.Persistence;
using Edp.Document.Api.Security;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "EDP Document API",
            Version = "v1",
            Description = "Enterprise Document Platform Document Service"
        };

        return Task.CompletedTask;
    });
});

builder.Services.AddSharedInfrastructure();
builder.Services.AddCurrentUserContext();
builder.Services.AddDocumentAuthorization(builder.Configuration);
builder.Services.AddDocumentApplication();
builder.Services.AddDocumentInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var serviceName = "Edp.Document.Api";
var meter = new Meter(serviceName);
var documentRequestsCounter = meter.CreateCounter<long>("edp.document.http.requests");
var documentRequestDuration = meter.CreateHistogram<double>("edp.document.http.duration_ms");

builder.Services.AddSingleton(meter);
builder.Services.AddSingleton(documentRequestsCounter);
builder.Services.AddSingleton(documentRequestDuration);

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
    using var activity = new System.Diagnostics.ActivitySource(serviceName).StartActivity("DocumentRequest");
    activity?.SetTag("correlation.id", correlationId);
    context.Items["document.request.startedAtUtc"] = DateTimeOffset.UtcNow;
    await next();
    var startedAt = context.Items["document.request.startedAtUtc"] as DateTimeOffset? ?? DateTimeOffset.UtcNow;
    var elapsedMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
    documentRequestsCounter.Add(1,
        new KeyValuePair<string, object?>("route", context.Request.Path.Value ?? "unknown"),
        new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));
    documentRequestDuration.Record(elapsedMs,
        new KeyValuePair<string, object?>("route", context.Request.Path.Value ?? "unknown"));
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Document API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
    app.MapGet("/", () => Results.Redirect("/scalar"));
}

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();
