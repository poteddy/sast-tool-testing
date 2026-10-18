using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ToolTester.Infrastructure.Persistance;

public sealed class AppDbContextFactory
      : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();

        options.UseSqlite(
            "Data Source=tooltester.db");

        return new ApplicationDbContext(options.Options);
    }
}

