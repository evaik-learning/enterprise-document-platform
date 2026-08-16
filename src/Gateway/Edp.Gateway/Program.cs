using Edp.Gateway.Extensions;
using Edp.Shared.Infrastructure.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddGatewayOptions(builder.Configuration);
builder.Services.AddGatewayApi();
builder.Services.AddGatewaySecurity(builder.Configuration);
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddGatewayServiceClients();
builder.Services.AddGatewayObservability();

var app = builder.Build();

app.UseSharedPlatformMiddleware();
app.UseGatewayPlatformMiddleware();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Enterprise Document Platform Gateway")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGatewayHealthChecks();

app.Run();

public partial class Program;
