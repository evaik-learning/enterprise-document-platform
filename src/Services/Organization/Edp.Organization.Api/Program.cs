using Edp.Organization.Application.Interfaces;
using Edp.Organization.Application.Repositories;
using Edp.Organization.Application.Services;
using Edp.Organization.Infrastructure.Persistence;
using Edp.Organization.Infrastructure.Repositories;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("OrganizationDb")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=OrganizationDb;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<OrganizationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSharedInfrastructure();
builder.Services.AddCurrentUserContext();
builder.Services.AddUnitOfWork<OrganizationDbContext>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();

var app = builder.Build();

app.UseSharedPlatformMiddleware();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Organization API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", async (OrganizationDbContext dbContext) =>
{
    try
    {
        await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Organization DB unavailable");
    }
});

app.MapControllers();

app.Run();

public partial class Program;
