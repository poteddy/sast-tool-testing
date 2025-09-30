using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain;
using ToolTester.Domain.Entities;

namespace ToolTester.Infrastructure.Persistance;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<CWECatalog> CWECatalogs { get; set; }

    public DbSet<Relationssship> RelationsShips { get; set; }
    public DbSet<CWETestResult> CWETestResults { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       
    }


    // ... other DbSets

}

public class BloggingContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseInMemoryDatabase("ToolTester");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}