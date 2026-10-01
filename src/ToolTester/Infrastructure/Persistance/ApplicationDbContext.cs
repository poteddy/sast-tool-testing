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

    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<CWETestResult> CWETestResults { get; set; }
    public DbSet<JulietCoverage> JulietCoverages { get; set; }

    public DbSet<Report> Reports { get; set; }
    public DbSet<Scan> Scans { get; set; }
    public DbSet<Tool> Tools { get; set; }

    public DbSet<CweSemanticRule> CweSemanticRules { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       
    }


    protected override void OnModelCreating(
      ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCweCatalog(modelBuilder);
        ConfigureJulietCoverage(modelBuilder);
        ConfigureRelationship(modelBuilder);
        ConfigureCweSemanticRule(modelBuilder);
        ConfigureTool(modelBuilder);
        ConfigureScan(modelBuilder);
        ConfigureCweTestResult(modelBuilder);
        ConfigureReport(modelBuilder);

#if DEBUG
        ValidateNoShadowForeignKeys(modelBuilder);
#endif
    }

    private static void ConfigureCweCatalog(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CWECatalog>(entity =>
        {
            entity.HasKey(catalog => catalog.Id);

            entity.HasIndex(catalog => catalog.CweId)
                .IsUnique();
        });
    }

    private static void ConfigureJulietCoverage(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JulietCoverage>(entity =>
        {
            entity.HasKey(coverage => coverage.Id);

            entity.HasIndex(coverage => coverage.CweId)
                .IsUnique();
        });
    }

    private static void ConfigureRelationship(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Relationship>(entity =>
        {
            entity.HasKey(relationship =>
                relationship.Id);

            entity.HasIndex(relationship => new
            {
                relationship.CweId,
                relationship.RelatedCweID,
                relationship.Nature,
                relationship.ViewId,
                relationship.IsDerived
            })
                .IsUnique();
        });
    }

    private static void ConfigureCweSemanticRule(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CweSemanticRule>(entity =>
        {
            entity.HasKey(rule => rule.Id);

            entity.HasIndex(rule => new
            {
                rule.SourceCweId,
                rule.TargetCweId,
                rule.Relationship,
                rule.ScannerRuleId,
                rule.ProgrammingLanguage,
                rule.Version
            })
                .IsUnique();
        });
    }

    private static void ConfigureTool(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tool>(entity =>
        {
            entity.HasKey(tool => tool.Id);

            entity.Property(tool => tool.Name)
                .IsRequired();

            entity.Property(tool => tool.Format)
                .IsRequired();

            /*
             * Tool.Scans <-> Scan.Tool
             *
             * Scan.ToolId is the only FK for this relationship.
             */
            entity.HasMany(tool => tool.Scans)
                .WithOne(scan => scan.Tool)
                .HasForeignKey(scan => scan.ToolId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            /*
             * Do not configure Tool.Reports here.
             * It is configured once from the Report side below.
             */
        });
    }

    private static void ConfigureScan(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scan>(entity =>
        {
            entity.HasKey(scan => scan.Id);

            entity.HasIndex(scan => scan.ToolId);

            /*
             * Do not configure Scan.TestResults or Scan.Reports here.
             * Those relationships are configured once from their
             * dependent entity configurations below.
             */
        });
    }

    private static void ConfigureCweTestResult(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CWETestResult>(entity =>
        {
            entity.HasKey(result => result.Id);

            /*
             * CWETestResult.Scan <-> Scan.TestResults
             *
             * CWETestResult.ScanId is the only FK.
             */
            entity.HasOne(result => result.Scan)
                .WithMany(scan => scan.TestResults)
                .HasForeignKey(result => result.ScanId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(result => result.ScanId);

            entity.HasIndex(result => new
            {
                result.ScanId,
                result.Test
            });
        });
    }

    private static void ConfigureReport(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(report => report.Id);

            /*
             * Report.Scan <-> Scan.Reports
             *
             * Explicitly specifying both navigation properties prevents
             * EF Core from inferring a separate relationship using
             * a shadow ScanId1 property.
             */
            entity.HasOne(report => report.Scan)
                .WithMany(scan => scan.Reports)
                .HasForeignKey(report => report.ScanId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            /*
             * Report.Tool <-> Tool.Reports
             *
             * Explicitly specifying both navigation properties prevents
             * EF Core from inferring a separate relationship using
             * a shadow ToolId1 property.
             */
            entity.HasOne(report => report.Tool)
                .WithMany(tool => tool.Reports)
                .HasForeignKey(report => report.ToolId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(report => report.Relationship)
                .IsRequired();

            entity.Property(report =>
                    report.TopologyRelationship)
                .IsRequired();

            entity.Property(report =>
                    report.GroundTruthAbstraction)
                .IsRequired();

            entity.Property(report =>
                    report.ScannerAbstraction)
                .IsRequired();

            entity.HasIndex(report => report.ScanId);

            entity.HasIndex(report => report.ToolId);

            entity.HasIndex(report => new
            {
                report.ScanId,
                report.ToolId
            });

            entity.HasIndex(report => new
            {
                report.ScanId,
                report.GroundTruthCweId,
                report.ScannerCweId,
                report.RootCauseCweId
            });
        });
    }

    private static void ValidateNoShadowForeignKeys(
        ModelBuilder modelBuilder)
    {
        var shadowForeignKeys =
            modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(entityType =>
                    entityType.GetForeignKeys())
                .SelectMany(foreignKey =>
                    foreignKey.Properties)
                .Where(property =>
                    property.IsShadowProperty())
                .Select(property =>
                    $"{property.DeclaringType.DisplayName()}." +
                    $"{property.Name}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(
                    propertyName => propertyName,
                    StringComparer.Ordinal)
                .ToList();

        if (shadowForeignKeys.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Unexpected EF Core shadow foreign-key properties: " +
            string.Join(", ", shadowForeignKeys));
    }

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