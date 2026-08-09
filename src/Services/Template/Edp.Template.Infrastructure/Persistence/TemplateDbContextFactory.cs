using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Edp.Template.Infrastructure.Persistence;

public class TemplateDbContextFactory : IDesignTimeDbContextFactory<TemplateDbContext>
{
    public TemplateDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TemplateDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("TemplateDb_Connection") ?? "Server=(localdb)\\MSSQLLocalDB;Database=TemplateDb;Trusted_Connection=True;";
        builder.UseSqlServer(connectionString);
        return new TemplateDbContext(builder.Options);
    }
}
