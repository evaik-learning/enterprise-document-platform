using Edp.Gateway.Extensions;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Edp.Shared.Observability.Serilog;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedInfrastructure();
builder.Services.AddSharedSerilog(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddGatewayOptions(builder.Configuration);
builder.Services.AddGatewayApi();
builder.Services.AddGatewaySecurity(builder.Configuration);
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddGatewayServiceClients();
builder.Services.AddGatewayCors(builder.Configuration);
builder.Services.AddGatewayObservability();

var app = builder.Build();

if (app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Edp.Gateway.Configuration.GatewayOptions>>().Value.DevAuth.Enabled)
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Gateway:DevAuth is a local-only sign-in and must not be enabled outside the Development environment.");
    app.Logger.LogWarning("Gateway DevAuth is ENABLED: demo users can sign in without Entra ID. Local development only.");
}

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
app.UseCors(Edp.Gateway.Extensions.ServiceCollectionExtensions.CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGatewayHealthChecks();

app.Run();

public partial class Program;
