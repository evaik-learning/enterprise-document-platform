using Edp.Audit.Application.Interfaces;
using Edp.Audit.Application.Repositories;
using Edp.Audit.Application.Services;
using Edp.Audit.Infrastructure.Persistence;
using Edp.Audit.Infrastructure.Repositories;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("AuditDb")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=AuditDb;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSharedInfrastructure();
builder.Services.AddCurrentUserContext();
builder.Services.AddUnitOfWork<AuditDbContext>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

var app = builder.Build();

app.UseSharedPlatformMiddleware();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Audit API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", async (AuditDbContext dbContext) =>
{
    try
    {
        await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Audit DB unavailable");
    }
});

app.MapControllers();

app.Run();

public partial class Program;
