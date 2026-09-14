using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Edp.Workflow.Application;
using Edp.Workflow.Infrastructure;
using Edp.Workflow.Infrastructure.Persistence;
using Edp.Persistence;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;

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
builder.Services.AddCurrentUserContext();
builder.Services.AddSharedJwtBearerAuthentication(builder.Configuration);
builder.Services.AddWorkflowApplication(builder.Configuration);
builder.Services.AddWorkflowInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSharedPlatformMiddleware();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/", () => Results.Ok("Workflow service is running."));
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();
