using Edp.Identity.Application.Interfaces;
using Edp.Identity.Application.Repositories;
using Edp.Identity.Application.Services;
using Edp.Identity.Infrastructure.Persistence;
using Edp.Persistence;
using Edp.Identity.Infrastructure.Repositories;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "EDP Identity API",
            Version = "v1",
            Description = "Enterprise Document Platform Identity Service"
        };

        return Task.CompletedTask;
    });
});

var connectionString = builder.Configuration.GetConnectionString("EdpDb")
    ?? throw new InvalidOperationException("Connection string 'EdpDb' is not configured.");

builder.Services.AddDbContext<EdpDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSharedInfrastructure();
builder.Services.AddCurrentUserContext();
builder.Services.AddSharedJwtBearerAuthentication(builder.Configuration);
builder.Services.AddUnitOfWork<EdpDbContext>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IIdentityService, IdentityService>();

var app = builder.Build();

app.UseSharedPlatformMiddleware();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Identity API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
    app.MapGet("/", () => Results.Redirect("/scalar"));
}

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", async (EdpDbContext dbContext) =>
{
    try
    {
        await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Identity DB unavailable");
    }
});

app.MapControllers();

app.Run();

public partial class Program;
