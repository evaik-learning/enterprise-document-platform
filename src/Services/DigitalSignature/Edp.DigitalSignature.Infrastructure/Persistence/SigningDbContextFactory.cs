namespace Edp.DigitalSignature.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory for SigningDbContext.
/// Used by Entity Framework Core tools (migrations) to create a DbContext instance without needing DI container.
/// </summary>
public class SigningDbContextFactory : IDesignTimeDbContextFactory<SigningDbContext>
{
    /// <summary>
    /// Creates a new SigningDbContext instance for design-time operations.
    /// </summary>
    public SigningDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SigningDbContext>();

        // Get connection string from environment or use a default for local development
        var connectionString = Environment.GetEnvironmentVariable("SigningDbConnection")
            ?? "Server=.;Database=EnterpriseDocumentPlatform_Signing;Integrated Security=true;TrustServerCertificate=true;";

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(GetType().Assembly.GetName().Name);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });

        return new SigningDbContext(optionsBuilder.Options);
    }
}
