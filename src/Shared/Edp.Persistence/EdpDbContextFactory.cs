using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Edp.Persistence;

public sealed class EdpDbContextFactory : IDesignTimeDbContextFactory<EdpDbContext>
{
    public EdpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("EdpDb_Connection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=EdpDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<EdpDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(EdpDbContext).Assembly.FullName))
            .Options;

        return new EdpDbContext(options);
    }
}
