using System.Security.Claims;
using System.Text;
using Azure.Storage.Blobs;
using Edp.Shared.Contracts;
using Edp.Shared.Infrastructure.Cache;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Infrastructure.Middleware;
using Edp.Shared.Infrastructure.Persistence;
using Edp.Shared.Security.CurrentUser;
using Edp.Shared.Storage;
using Edp.Shared.Storage.Abstractions;
using Edp.SharedKernel.Domain;
using Edp.SharedKernel.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Edp.Gateway.Tests;

public class SharedFoundationTests
{
    [Fact]
    public void AuditableEntity_ShouldTrackAuditFields()
    {
        var entity = new TestAuditableEntity(Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.True(entity.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void DateTimeProvider_ShouldReturnUtcNow()
    {
        var provider = new SystemDateTimeProvider();

        Assert.True(provider.UtcNow <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CurrentUser_ShouldBeAnonymousByDefault()
    {
        var currentUser = CurrentUser.Anonymous;

        Assert.False(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Empty, currentUser.UserId);
    }

    [Fact]
    public void CurrentUser_FromClaimsPrincipal_ShouldMapAuthenticationContext()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "alice@example.com"),
            new Claim(ClaimTypes.Email, "alice@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("tenant_id", "tenant-123"),
            new Claim("org_id", Guid.NewGuid().ToString())
        ], "oidc"));

        var currentUser = CurrentUser.FromClaimsPrincipal(principal);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal("alice@example.com", currentUser.UserName);
        Assert.Contains("Admin", currentUser.Roles);
    }

    [Fact]
    public void CurrentOrganization_FromClaimsPrincipal_ShouldMapOrganizationId()
    {
        var organizationId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("organization_id", organizationId.ToString())
        ], "oidc"));

        var currentOrganization = CurrentOrganization.FromClaimsPrincipal(principal);

        Assert.True(currentOrganization.IsInOrganization);
        Assert.Equal(organizationId, currentOrganization.OrganizationId);
    }

    [Fact]
    public void AddCurrentUserContext_ShouldResolveCurrentUserAndOrganizationFromHttpContext()
    {
        var services = new ServiceCollection();
        services.AddCurrentUserContext();

        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "alice@example.com"),
                new Claim(ClaimTypes.Email, "alice@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("organization_id", organizationId.ToString())
            ], "oidc"))
        };

        var currentUser = provider.GetRequiredService<ICurrentUser>();
        var currentOrganization = provider.GetRequiredService<ICurrentOrganization>();

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal("alice@example.com", currentUser.UserName);
        Assert.Contains("Admin", currentUser.Roles);

        Assert.True(currentOrganization.IsInOrganization);
        Assert.Equal(organizationId, currentOrganization.OrganizationId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldSetHeaderWhenMissing()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";
        var loggerFactory = LoggerFactory.Create(_ => { });
        var logger = loggerFactory.CreateLogger<CorrelationIdMiddleware>();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        Assert.Equal("trace-123", context.Items["X-Correlation-ID"]?.ToString());
        Assert.Equal("trace-123", context.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Fact]
    public async Task AzureBlobStorageService_ShouldUploadAndDownloadContent()
    {
        var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var client = new BlobServiceClient(connectionString);
        var service = new AzureBlobStorageService(client, "tests");
        var path = $"shared-tests/{Guid.NewGuid():N}.txt";
        var content = "hello from blob storage test";
        var uploaded = await service.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), path, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(uploaded));

        using var downloaded = await service.DownloadAsync(path, CancellationToken.None);
        Assert.NotNull(downloaded);

        using var reader = new StreamReader(downloaded!);
        var actual = await reader.ReadToEndAsync();

        Assert.Equal(content, actual);

        await service.DeleteAsync(path, CancellationToken.None);
    }

    [Fact]
    public async Task CacheService_ShouldStoreAndRetrieveValues()
    {
        var cache = new InMemoryCacheService();

        await cache.SetAsync("phase4", "done", CancellationToken.None);

        var exists = await cache.ExistsAsync("phase4", CancellationToken.None);
        var value = await cache.GetAsync<string>("phase4", CancellationToken.None);

        Assert.True(exists);
        Assert.Equal("done", value);

        await cache.RemoveAsync("phase4", CancellationToken.None);
        Assert.False(await cache.ExistsAsync("phase4", CancellationToken.None));
    }

    [Fact]
    public void EventEnvelope_ShouldCaptureMetadata()
    {
        var envelope = new EventEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = "OrganizationCreated",
            OrganizationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            Version = 1,
            Data = new { Name = "Contoso" }
        };

        Assert.Equal("OrganizationCreated", envelope.EventType);
        Assert.Equal(1, envelope.Version);
        Assert.Equal("Contoso", envelope.Data?.GetType().GetProperty("Name")?.GetValue(envelope.Data)?.ToString());
    }

    [Fact]
    public void UnitOfWork_ShouldExistForDbContext()
    {
        var type = typeof(UnitOfWork<TestUnitOfWorkDbContext>);

        Assert.Contains("UnitOfWork", type.Name);
        Assert.True(typeof(IUnitOfWork).IsAssignableFrom(type));
    }

    private sealed class TestAuditableEntity : AuditableEntity<Guid>
    {
        public TestAuditableEntity(Guid id)
        {
            Id = id;
        }
    }

    private sealed class TestUnitOfWorkDbContext : DbContext
    {
    }
}
