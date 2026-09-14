using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Edp.Template.Infrastructure.Persistence;

public class TemplateDbContextFactory : IDesignTimeDbContextFactory<TemplateDbContext>
{
    public TemplateDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TemplateDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("EdpDb_Connection") ?? "Server=(localdb)\\MSSQLLocalDB;Database=EdpDb;Trusted_Connection=True;";
        builder.UseSqlServer(connectionString);
        return new TemplateDbContext(builder.Options);
    }
}
