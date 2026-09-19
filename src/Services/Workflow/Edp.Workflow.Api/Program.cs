using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Edp.Workflow.Application;
using Edp.Workflow.Infrastructure;
using Edp.Workflow.Infrastructure.Persistence;
using Edp.Persistence;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Edp.Workflow.Api.Middleware;
using Edp.Workflow.Api.Security;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "EDP Workflow API",
            Version = "v1",
            Description = "Enterprise Document Platform Workflow Service"
        };

        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddSharedInfrastructure();
builder.Services.AddSharedJwtBearerAuthentication(builder.Configuration);
builder.Services.AddCurrentUserContext();
builder.Services.AddWorkflowAuthorization(builder.Configuration);
builder.Services.AddWorkflowApplication(builder.Configuration);
builder.Services.AddWorkflowInfrastructure(builder.Configuration);

const string serviceName = "Edp.Workflow.Api";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName,
        typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddMeter("Edp.Workflow");
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddSource("Edp.Workflow");
    });

var app = builder.Build();

app.UseSharedPlatformMiddleware();
app.UseMiddleware<WorkflowExceptionMappingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/", () => Results.Ok("Workflow service is running."));
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();
