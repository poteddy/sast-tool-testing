using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;

namespace ToolTester.Infrastructure.Persistance;

public class ApplicationDbContext :
    DbContext,
    IApplicationDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CWECatalog> CWECatalogs { get; set; }

    public DbSet<Relationship> Relationships { get; set; }

    public DbSet<CWETestResult> CWETestResults { get; set; }

    public DbSet<JulietCoverage> JulietCoverages { get; set; }

    public DbSet<JulietTest> JulietTests { get; set; }

    public DbSet<Report> Reports { get; set; }

    public DbSet<Scan> Scans { get; set; }

    public DbSet<Tool> Tools { get; set; }

    public DbSet<CweSemanticRule> CweSemanticRules { get; set; }

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCweCatalog(modelBuilder);
        ConfigureJulietCoverage(modelBuilder);
        ConfigureJulietTest(modelBuilder);
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

            /*
             * CweId is used as the principal key for the
             * screenshot relationships.
             */
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

            /*
             * Screenshot relationship:
             *
             * CWECatalog.CweId 1 <-> 1 JulietCoverage.CweId
             *
             * Navigation-free configuration keeps the entity
             * classes unchanged.
             */
            entity.HasOne<CWECatalog>()
                .WithOne()
                .HasForeignKey<JulietCoverage>(
                    coverage => coverage.CweId)
                .HasPrincipalKey<CWECatalog>(
                    catalog => catalog.CweId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureJulietTest(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JulietTest>(entity =>
        {
            entity.HasKey(test => test.Id);

            entity.Property(test => test.NormalizedPath)
                .IsRequired();

            entity.Property(test => test.FileName)
                .IsRequired();

            /*
             * A normalized path represents exactly one known
             * Juliet test/file.
             */
            entity.HasIndex(test => test.NormalizedPath)
                .IsUnique();

            /*
             * Useful for querying all tests belonging to a CWE.
             */
            entity.HasIndex(test => test.CweId);

            /*
             * Screenshot relationship:
             *
             * CWECatalog.CweId 1 <-> * JulietTest.CweId
             */
            entity.HasOne<CWECatalog>()
                .WithMany()
                .HasForeignKey(test => test.CweId)
                .HasPrincipalKey(catalog => catalog.CweId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
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

            /*
             * Screenshot relationship:
             *
             * CWECatalog.CweId 1 <-> * Relationship.CweId
             *
             * Represents the source CWE.
             */
            entity.HasOne<CWECatalog>()
                .WithMany()
                .HasForeignKey(relationship =>
                    relationship.CweId)
                .HasPrincipalKey(catalog =>
                    catalog.CweId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            /*
             * Screenshot relationship:
             *
             * CWECatalog.CweId 1 <-> *
             * Relationship.RelatedCweID
             *
             * Represents the target/related CWE.
             */
            entity.HasOne<CWECatalog>()
                .WithMany()
                .HasForeignKey(relationship =>
                    relationship.RelatedCweID)
                .HasPrincipalKey(catalog =>
                    catalog.CweId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
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
                rule.ScannerFoundCweId,
                rule.TestTargetCwe,
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
             * Scan.ToolId is the FK.
             */
            entity.HasMany(tool => tool.Scans)
                .WithOne(scan => scan.Tool)
                .HasForeignKey(scan => scan.ToolId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
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
             * Scan.TestResults and Scan.Reports are configured
             * from their dependent entities.
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
             */
            entity.HasOne(result => result.Scan)
                .WithMany(scan => scan.TestResults)
                .HasForeignKey(result => result.ScanId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            /*
             * CWETestResult.Test <-> JulietTest.Results
             *
             * TestId identifies the canonical Juliet test.
             *
             * CWETestResult.FilePath remains the original path
             * reported by the scanner.
             */
            entity.HasOne(result => result.Test)
                .WithMany(test => test.Results)
                .HasForeignKey(result => result.TestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(result => result.ScanId);

            entity.HasIndex(result => result.TestId);

            /*
             * Important for finding all scanner findings
             * associated with a specific test in a scan.
             *
             * This is intentionally NOT unique because one
             * scanner could report multiple findings for the
             * same file/test.
             */
            entity.HasIndex(result => new
            {
                result.ScanId,
                result.TestId
            });

            /*
             * Useful for analyzing what CWE scanners reported
             * for a particular Juliet test.
             */
            entity.HasIndex(result => new
            {
                result.TestId,
                result.ScannerFoundCWE
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
             */
            entity.HasOne(report => report.Scan)
                .WithMany(scan => scan.Reports)
                .HasForeignKey(report => report.ScanId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            /*
             * Report.Tool <-> Tool.Reports
             */
            entity.HasOne(report => report.Tool)
                .WithMany(tool => tool.Reports)
                .HasForeignKey(report => report.ToolId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            /*
             * IMPORTANT:
             *
             * GroundTruthCweId is intentionally NOT configured
             * as a foreign key to JulietCoverage.CweId.
             *
             * A Report may legitimately reference a CWE that
             * exists in CWECatalog but is not represented in the
             * Juliet test suite / JulietCoverage table.
             *
             * Power BI may create:
             *
             * JulietCoverage.CweId
             *      1 -> *
             * Report.GroundTruthCweId
             *
             * as an analytical model relationship without making
             * it a relational database constraint.
             */

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

public class BloggingContextFactory :
    IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(
        string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseInMemoryDatabase("ToolTester");

        return new ApplicationDbContext(
            optionsBuilder.Options);
    }
}