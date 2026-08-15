using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "EDP Organization API",
            Version = "v1",
            Description = "Enterprise Document Platform Organization Service"
        };

        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapOpenApi();
app.MapGet("/", () => Results.Ok("Organization service is running."));
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();
