using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Application.CweRelationshipEngine;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Extensions;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class ApplicationDbContextSeed
    : IApplicationDbContextSeed
{
    private const string MitreSource = "MITRE";
    private const string MitreCategorySource = "MITRE Category";
    private const string DerivedInverseSource = "DerivedInverse";
    private const string UnspecifiedOrdinal = "Unspecified";
    private const string CategoryAbstraction = "Category";

    private static readonly SemaphoreSlim SeedLock =
        new(initialCount: 1, maxCount: 1);

    private static bool _seedCompleted;

    private readonly IConfiguration _configuration;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    private readonly IZipfileService _zipfileService;

    private readonly IJulietPathNormalizer
        _julietPathNormalizer;

    public ApplicationDbContextSeed(
        IConfiguration configuration,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IZipfileService zipfileService,
        IJulietPathNormalizer julietPathNormalizer)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(
                nameof(configuration));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));

        _zipfileService = zipfileService
            ?? throw new ArgumentNullException(
                nameof(zipfileService));

        _julietPathNormalizer = julietPathNormalizer
            ?? throw new ArgumentNullException(
                nameof(julietPathNormalizer));
    }

    public async Task SeedCWECatalog()
    {
        if (_seedCompleted)
        {
            return;
        }

        await SeedLock.WaitAsync();

        try
        {
            if (_seedCompleted)
            {
                return;
            }

            await SeedCweCatalogAsync(
                CancellationToken.None);

            _seedCompleted = true;
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private async Task SeedCweCatalogAsync(
        CancellationToken cancellationToken)
    {
        var semanticRules =
            CreateJulietSemanticRules();

        await ImportMitreCatalogAsync(
            semanticRules,
            cancellationToken);

        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        await SeedJulietCoverageAsync(
            context,
            cancellationToken);

        await SeedToolsAsync(
            context,
            cancellationToken);
    }

    private async Task ImportMitreCatalogAsync(
        IReadOnlyCollection<SemanticRule> semanticRules,
        CancellationToken cancellationToken)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        var settings = _configuration
            .GetRequiredSection("MitreCatalogSetting")
            .Get<MitreCatalogSetting>()
            ?? throw new InvalidOperationException(
                "MitreCatalogSetting is missing.");

        ValidateRequiredFile(
            settings.Path,
            "MITRE CWE catalog");

        var weaknessCatalog =
            XMLExtensions.ReadXML(settings.Path);

        if (context.Database.IsInMemory())
        {
            await ImportCweDataAsync(
                context,
                weaknessCatalog,
                semanticRules,
                cancellationToken);

            return;
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await ImportCweDataAsync(
                context,
                weaknessCatalog,
                semanticRules,
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static async Task ImportCweDataAsync(
        ApplicationDbContext context,
        Weakness_Catalog weaknessCatalog,
        IReadOnlyCollection<SemanticRule> semanticRules,
        CancellationToken cancellationToken)
    {
        /*
         * Always run this upsert.
         *
         * It imports both MITRE weakness entries and category
         * entries such as CWE-398.
         */
        await UpsertCweNodesAsync(
            context,
            weaknessCatalog,
            cancellationToken);

        if (!await context.Relationships
                .AnyAsync(cancellationToken))
        {
            await UpsertDirectRelationshipsAsync(
                context,
                weaknessCatalog,
                cancellationToken);
        }

        if (!await context.Relationships.AnyAsync(
                relationship => relationship.IsDerived,
                cancellationToken))
        {
            await AddInverseRelationshipsAsync(
                context,
                cancellationToken);
        }

        if (!await context.CweSemanticRules
                .AnyAsync(cancellationToken))
        {
            await UpsertSemanticRulesAsync(
                context,
                semanticRules,
                cancellationToken);
        }
    }

    private static async Task UpsertCweNodesAsync(
        ApplicationDbContext context,
        Weakness_Catalog catalog,
        CancellationToken cancellationToken)
    {
        var existingByCweId =
            await context.CWECatalogs
                .ToDictionaryAsync(
                    catalogEntry => catalogEntry.CweId,
                    cancellationToken);

        /*
         * Import standard MITRE weakness entries.
         */
        foreach (var weakness in catalog.Weaknesses)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!int.TryParse(
                    weakness.ID,
                    out var cweId))
            {
                continue;
            }

            var abstraction =
                CweAbstractionParser
                    .Parse(
                        weakness.Abstraction.ToString())
                    .ToString();

            var status =
                weakness.Status.ToString();

            var name =
                weakness.Name ?? string.Empty;

            var description =
                weakness.Description ?? string.Empty;

            if (existingByCweId.TryGetValue(
                    cweId,
                    out var existing))
            {
                existing.Name =
                    name;

                existing.Description =
                    description;

                existing.Abstraction =
                    abstraction;

                existing.Status =
                    status;

                continue;
            }

            var cwe = new CWECatalog
            {
                CweId = cweId,
                Name = name,
                Description = description,
                Abstraction = abstraction,
                Status = status
            };

            context.CWECatalogs.Add(cwe);

            existingByCweId.Add(
                cweId,
                cwe);
        }

        /*
         * Import MITRE category entries.
         *
         * Juliet contains tests whose folder CWE is a category,
         * such as CWE-398. Categories are retained as valid
         * benchmark ground-truth values.
         */
        if (catalog.Categories is not null)
        {
            foreach (var category in catalog.Categories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!int.TryParse(
                        category.ID,
                        out var cweId))
                {
                    continue;
                }

                var name =
                    category.Name ?? string.Empty;

                var description =
                    category.Summary?.ToString()
                    ?? string.Empty;

                var status =
                    category.Status.ToString();

                if (existingByCweId.TryGetValue(
                        cweId,
                        out var existing))
                {
                    /*
                     * Do not overwrite a normal weakness with
                     * category metadata if the same ID somehow
                     * appears in both collections.
                     */
                    if (!string.Equals(
                            existing.Abstraction,
                            CategoryAbstraction,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    existing.Name =
                        name;

                    existing.Description =
                        description;

                    existing.Abstraction =
                        CategoryAbstraction;

                    existing.Status =
                        status;

                    continue;
                }

                var cweCategory = new CWECatalog
                {
                    CweId = cweId,
                    Name = name,
                    Description = description,
                    Abstraction = CategoryAbstraction,
                    Status = status
                };

                context.CWECatalogs.Add(
                    cweCategory);

                existingByCweId.Add(
                    cweId,
                    cweCategory);

                Debug.WriteLine(
                    $"Prepared MITRE category CWE-{cweId}: " +
                    $"{name} ({MitreCategorySource})");
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static async Task UpsertDirectRelationshipsAsync(
        ApplicationDbContext context,
        Weakness_Catalog catalog,
        CancellationToken cancellationToken)
    {
        var validCweIds =
            await context.CWECatalogs
                .AsNoTracking()
                .Select(catalogEntry =>
                    catalogEntry.CweId)
                .ToHashSetAsync(
                    cancellationToken);

        var existingRelationshipData =
            await context.Relationships
                .AsNoTracking()
                .Where(relationship =>
                    !relationship.IsDerived)
                .Select(relationship => new
                {
                    relationship.CweId,
                    relationship.RelatedCweID,
                    relationship.Nature,
                    relationship.ViewId,
                    relationship.IsDerived
                })
                .ToListAsync(
                    cancellationToken);

        var existingKeys =
            existingRelationshipData
                .Select(relationship =>
                    new RelationshipKey(
                        relationship.CweId,
                        relationship.RelatedCweID,
                        relationship.Nature,
                        relationship.ViewId,
                        relationship.IsDerived))
                .ToHashSet();

        foreach (var weakness in catalog.Weaknesses)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!int.TryParse(
                    weakness.ID,
                    out var sourceCweId))
            {
                continue;
            }

            if (!validCweIds.Contains(sourceCweId))
            {
                continue;
            }

            if (weakness.Related_Weaknesses is null)
            {
                continue;
            }

            foreach (var related in
                     weakness.Related_Weaknesses)
            {
                if (!int.TryParse(
                        related.CWE_ID,
                        out var targetCweId))
                {
                    continue;
                }

                if (!validCweIds.Contains(targetCweId))
                {
                    continue;
                }

                var nature =
                    related.Nature.ToString();

                var viewId =
                    ParseNullableInt(
                        related.View_ID?.ToString());

                var key =
                    new RelationshipKey(
                        sourceCweId,
                        targetCweId,
                        nature,
                        viewId,
                        IsDerived: false);

                if (!existingKeys.Add(key))
                {
                    continue;
                }

                context.Relationships.Add(
                    new Relationship
                    {
                        CweId = sourceCweId,
                        RelatedCweID = targetCweId,
                        ChainId = related.Chain_ID,
                        Nature = nature,
                        ViewId = viewId,
                        OrderSpecified =
                            related.OrdinalSpecified,
                        Ordinal =
                            GetOrdinal(
                                related.OrdinalSpecified,
                                related.Ordinal.ToString()),
                        IsDerived = false,
                        Distance = 1,
                        Source = MitreSource
                    });
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static async Task AddInverseRelationshipsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var directRelationships =
            await context.Relationships
                .AsNoTracking()
                .Where(relationship =>
                    !relationship.IsDerived)
                .ToListAsync(
                    cancellationToken);

        var existingRelationshipData =
            await context.Relationships
                .AsNoTracking()
                .Select(relationship => new
                {
                    relationship.CweId,
                    relationship.RelatedCweID,
                    relationship.Nature,
                    relationship.ViewId,
                    relationship.IsDerived
                })
                .ToListAsync(
                    cancellationToken);

        var existingKeys =
            existingRelationshipData
                .Select(relationship =>
                    new RelationshipKey(
                        relationship.CweId,
                        relationship.RelatedCweID,
                        relationship.Nature,
                        relationship.ViewId,
                        relationship.IsDerived))
                .ToHashSet();

        foreach (var relationship in directRelationships)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inverseNature =
                GetInverseNature(
                    relationship.Nature);

            if (inverseNature is null)
            {
                continue;
            }

            var key =
                new RelationshipKey(
                    relationship.RelatedCweID,
                    relationship.CweId,
                    inverseNature,
                    relationship.ViewId,
                    IsDerived: true);

            if (!existingKeys.Add(key))
            {
                continue;
            }

            context.Relationships.Add(
                new Relationship
                {
                    CweId =
                        relationship.RelatedCweID,

                    RelatedCweID =
                        relationship.CweId,

                    Nature =
                        inverseNature,

                    ViewId =
                        relationship.ViewId,

                    ChainId =
                        relationship.ChainId,

                    OrderSpecified =
                        relationship.OrderSpecified,

                    Ordinal =
                        relationship.Ordinal
                        ?? UnspecifiedOrdinal,

                    IsDerived =
                        true,

                    Distance =
                        1,

                    Source =
                        DerivedInverseSource
                });
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static string? GetInverseNature(
        string nature)
    {
        return nature switch
        {
            nameof(RelatedNatureEnumeration.ChildOf) =>
                nameof(RelatedNatureEnumeration.ParentOf),

            nameof(RelatedNatureEnumeration.ParentOf) =>
                nameof(RelatedNatureEnumeration.ChildOf),

            nameof(RelatedNatureEnumeration.PeerOf) =>
                nameof(RelatedNatureEnumeration.PeerOf),

            nameof(RelatedNatureEnumeration.CanPrecede) =>
                nameof(RelatedNatureEnumeration.CanFollow),

            nameof(RelatedNatureEnumeration.CanFollow) =>
                nameof(RelatedNatureEnumeration.CanPrecede),

            _ => null
        };
    }

    private static async Task UpsertSemanticRulesAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<SemanticRule> rules,
        CancellationToken cancellationToken)
    {
        if (rules.Count == 0)
        {
            return;
        }

        var validCweIds =
            await context.CWECatalogs
                .AsNoTracking()
                .Select(catalog =>
                    catalog.CweId)
                .ToHashSetAsync(
                    cancellationToken);

        var existingRules =
            await context.CweSemanticRules
                .ToListAsync(
                    cancellationToken);

        var existingByKey =
            existingRules
                .GroupBy(CreateSemanticRuleKey)
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateSemanticRule(
                rule,
                validCweIds);

            var key =
                CreateSemanticRuleKey(rule);

            if (existingByKey.TryGetValue(
                    key,
                    out var existing))
            {
                UpdateSemanticRule(
                    existing,
                    rule);

                continue;
            }

            var entity =
                CreateSemanticRuleEntity(
                    rule);

            context.CweSemanticRules.Add(
                entity);

            existingByKey.Add(
                key,
                entity);
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static CweSemanticRule
        CreateSemanticRuleEntity(
            SemanticRule rule)
    {
        return new CweSemanticRule
        {
            ScannerFoundCweId =
                rule.ScannerFoundCweId,

            TestTargetCwe =
                rule.TestTargetCwe,

            Relationship =
                rule.Relationship.ToString(),

            Score =
                rule.Score,

            ScannerRuleId =
                NormalizeOptionalValue(
                    rule.ScannerRuleId),

            ProgrammingLanguage =
                NormalizeOptionalValue(
                    rule.ProgrammingLanguage),

            Rationale =
                rule.Rationale.Trim(),

            EvidenceReference =
                rule.EvidenceReference.Trim(),

            Bidirectional =
                rule.Bidirectional,

            Enabled =
                rule.Enabled,

            Version =
                rule.Version,

            IsCustom =
                false
        };
    }

    private static void UpdateSemanticRule(
        CweSemanticRule entity,
        SemanticRule import)
    {
        entity.Score =
            import.Score;

        entity.Rationale =
            import.Rationale.Trim();

        entity.EvidenceReference =
            import.EvidenceReference.Trim();

        entity.Bidirectional =
            import.Bidirectional;

        entity.Enabled =
            import.Enabled;

        entity.ScannerRuleId =
            NormalizeOptionalValue(
                import.ScannerRuleId);

        entity.ProgrammingLanguage =
            NormalizeOptionalValue(
                import.ProgrammingLanguage);
    }

    private static SemanticRuleKey CreateSemanticRuleKey(
        CweSemanticRule rule)
    {
        return new SemanticRuleKey(
            rule.ScannerFoundCweId,
            rule.TestTargetCwe,
            NormalizeRequiredValue(
                rule.Relationship),
            NormalizeOptionalValue(
                rule.ScannerRuleId),
            NormalizeOptionalValue(
                rule.ProgrammingLanguage),
            rule.Version);
    }

    private static SemanticRuleKey CreateSemanticRuleKey(
        SemanticRule rule)
    {
        return new SemanticRuleKey(
            rule.ScannerFoundCweId,
            rule.TestTargetCwe,
            NormalizeRequiredValue(
                rule.Relationship.ToString()),
            NormalizeOptionalValue(
                rule.ScannerRuleId),
            NormalizeOptionalValue(
                rule.ProgrammingLanguage),
            rule.Version);
    }

    private static void ValidateSemanticRule(
        SemanticRule rule,
        IReadOnlySet<int> validCweIds)
    {
        if (rule.ScannerFoundCweId <= 0)
        {
            throw new InvalidDataException(
                "Semantic rule ScannerFoundCweId must be " +
                "greater than zero.");
        }

        if (!validCweIds.Contains(
                rule.ScannerFoundCweId))
        {
            throw new InvalidDataException(
                $"Semantic rule scanner CWE-" +
                $"{rule.ScannerFoundCweId} does not exist " +
                "in the imported MITRE catalog.");
        }

        if (rule.TestTargetCwe <= 0)
        {
            throw new InvalidDataException(
                "Semantic rule TestTargetCwe must be " +
                "greater than zero.");
        }

        if (!validCweIds.Contains(
                rule.TestTargetCwe))
        {
            throw new InvalidDataException(
                $"Semantic rule target CWE-" +
                $"{rule.TestTargetCwe} does not exist " +
                "in the imported MITRE catalog.");
        }

        if (!Enum.IsDefined(rule.Relationship))
        {
            throw new InvalidDataException(
                $"Unknown semantic relationship " +
                $"'{rule.Relationship}' for CWE-" +
                $"{rule.ScannerFoundCweId} to CWE-" +
                $"{rule.TestTargetCwe}.");
        }

        if (rule.Score is < 0 or > 1000)
        {
            throw new InvalidDataException(
                "Semantic-rule score must be between 0 and " +
                $"1000. Received {rule.Score} for CWE-" +
                $"{rule.ScannerFoundCweId} to CWE-" +
                $"{rule.TestTargetCwe}.");
        }

        if (rule.Version <= 0)
        {
            throw new InvalidDataException(
                "Semantic rule Version must be greater " +
                "than zero.");
        }

        if (string.IsNullOrWhiteSpace(
                rule.Rationale))
        {
            throw new InvalidDataException(
                "Semantic-rule rationale is required for " +
                $"CWE-{rule.ScannerFoundCweId} to CWE-" +
                $"{rule.TestTargetCwe}.");
        }

        if (string.IsNullOrWhiteSpace(
                rule.EvidenceReference))
        {
            throw new InvalidDataException(
                "Semantic-rule evidence reference is required " +
                $"for CWE-{rule.ScannerFoundCweId} to CWE-" +
                $"{rule.TestTargetCwe}.");
        }
    }

    private async Task SeedJulietCoverageAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var hasJulietTests =
            await context.JulietTests
                .AsNoTracking()
                .AnyAsync(
                    cancellationToken);

        var hasJulietCoverage =
            await context.JulietCoverages
                .AsNoTracking()
                .AnyAsync(
                    cancellationToken);

        if (hasJulietTests &&
            hasJulietCoverage)
        {
            return;
        }

        if (hasJulietTests ||
            hasJulietCoverage)
        {
            throw new InvalidOperationException(
                "The Juliet seed is incomplete. JulietTests and " +
                "JulietCoverages must either both be empty or both " +
                "contain data. Delete the partial Juliet data " +
                "before running the seed again.");
        }

        var settings = _configuration
            .GetRequiredSection("JulietProjectSetting")
            .Get<JulietProjectSetting>()
            ?? throw new InvalidOperationException(
                "JulietProjectSetting is missing.");

        if (string.IsNullOrWhiteSpace(
                settings.Path))
        {
            throw new InvalidOperationException(
                "The Juliet project path is empty.");
        }

        var julietFilePaths =
            _zipfileService.GetFilePaths(
                settings.Path);

        if (julietFilePaths.Count == 0)
        {
            throw new InvalidDataException(
                "No Juliet source files were found at the " +
                "configured project path.");
        }

        var validCweIds =
            await context.CWECatalogs
                .AsNoTracking()
                .Select(catalog =>
                    catalog.CweId)
                .ToHashSetAsync(
                    cancellationToken);

        if (validCweIds.Count == 0)
        {
            throw new InvalidOperationException(
                "The MITRE CWE catalog must be seeded before " +
                "the Juliet tests.");
        }

        var missingCweIds =
            new HashSet<int>();

        var normalizedPaths =
            new HashSet<string>(
                StringComparer.Ordinal);

        var julietTests =
            new List<JulietTest>();

        foreach (var filePath in julietFilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_julietPathNormalizer
                    .IsSupportedSourceFile(filePath))
            {
                continue;
            }

            var normalizedPath =
                _julietPathNormalizer.Normalize(
                    filePath);

            if (string.IsNullOrWhiteSpace(
                    normalizedPath))
            {
                Debug.WriteLine(
                    "Skipping Juliet file with an empty " +
                    $"normalized path: {filePath}");

                continue;
            }

            if (!normalizedPaths.Add(
                    normalizedPath))
            {
                Debug.WriteLine(
                    "Duplicate normalized Juliet path skipped: " +
                    normalizedPath);

                continue;
            }

            var cweId =
                _julietPathNormalizer.ExtractCweId(
                    normalizedPath);

            if (!cweId.HasValue)
            {
                Debug.WriteLine(
                    "No CWE could be extracted from Juliet path: " +
                    filePath);

                continue;
            }

            if (!validCweIds.Contains(
                    cweId.Value))
            {
                missingCweIds.Add(
                    cweId.Value);

                Debug.WriteLine(
                    $"MITRE CWE-{cweId.Value} was not found " +
                    $"for Juliet path: {filePath}");

                continue;
            }

            var julietTest =
                new JulietTest
                {
                    NormalizedPath =
                        normalizedPath,

                    FileName =
                        _julietPathNormalizer.GetFileName(
                            normalizedPath),

                    CweId =
                        cweId.Value,

                    Results =
                        []
                };

            julietTests.Add(
                julietTest);

            Debug.WriteLine(
                $"Prepared Juliet test: " +
                $"CWE={julietTest.CweId}, " +
                $"Path={julietTest.NormalizedPath}");
        }

        if (missingCweIds.Count > 0)
        {
            throw new InvalidDataException(
                "The following Juliet CWE IDs do not exist in " +
                "the MITRE catalog: " +
                string.Join(
                    ", ",
                    missingCweIds.Order()));
        }

        if (julietTests.Count == 0)
        {
            throw new InvalidDataException(
                "No valid Juliet source tests were found. " +
                "Verify the configured project path, source-file " +
                "extensions, and path normalization rules.");
        }

        var coverageRows =
            julietTests
                .GroupBy(test =>
                    test.CweId)
                .Select(group =>
                    new JulietCoverage
                    {
                        CweId =
                            group.Key,

                        Covered =
                            group.Count()
                    })
                .OrderBy(coverage =>
                    coverage.CweId)
                .ToList();

        context.JulietTests.AddRange(
            julietTests);

        context.JulietCoverages.AddRange(
            coverageRows);

        await context.SaveChangesAsync(
            cancellationToken);

        Debug.WriteLine(
            $"Seeded {julietTests.Count} Juliet tests and " +
            $"{coverageRows.Count} Juliet coverage rows.");
    }

    private IReadOnlyCollection<SemanticRule>
        CreateJulietSemanticRules()
    {
        var settings = _configuration
            .GetRequiredSection("JulietProjectSetting")
            .Get<JulietProjectSetting>()
            ?? throw new InvalidOperationException(
                "JulietProjectSetting is missing.");

        if (string.IsNullOrWhiteSpace(
                settings.Path))
        {
            throw new InvalidOperationException(
                "The Juliet project path is empty.");
        }

        return _zipfileService
            .GetJulietMappings(
                settings.Path)
            .GroupBy(mapping => new
            {
                mapping.PrimaryCweId,
                mapping.SecondaryCweId
            })
            .Select(group =>
                new SemanticRule(
                    ScannerFoundCweId:
                        group.Key.SecondaryCweId,

                    TestTargetCwe:
                        group.Key.PrimaryCweId,

                    Relationship:
                        CweRelationshipKind.JulietRootCause,

                    Score:
                        750,

                    Rationale:
                        "Observed in Juliet naming convention.",

                    EvidenceReference:
                        group.First().FileName))
            .ToList();
    }

    private static async Task SeedToolsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var desiredTools = new[]
        {
            new ToolSeed(
                Name: "Semgrep",
                Format: "JSON"),

            new ToolSeed(
                Name: "Sarif",
                Format: "Sarif"),

            new ToolSeed(
                Name: "Veracode",
                Format: "Veracode"),

            new ToolSeed(
                Name: "CPP Checker",
                Format: "Cpp Checker"),

            new ToolSeed(
                Name: "CheckMarx",
                Format: "CheckMarx"),

            new ToolSeed(
                Name: "Cycode",
                Format: "Cycode"),

            new ToolSeed(
                Name: "OpenVINO",
                Format: "OpenVINO"),

            new ToolSeed(
                Name: "GitLabSast",
                Format: "GitLabSast")
        };

        var existingToolNames =
            await context.Tools
                .AsNoTracking()
                .Select(tool =>
                    tool.Name)
                .ToHashSetAsync(
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);

        foreach (var desiredTool in desiredTools)
        {
            if (!existingToolNames.Add(
                    desiredTool.Name))
            {
                continue;
            }

            context.Tools.Add(
                new Tool
                {
                    Name =
                        desiredTool.Name,

                    Format =
                        desiredTool.Format
                });
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static void ValidateRequiredFile(
        string? filePath,
        string description)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            throw new InvalidOperationException(
                $"The {description} path is empty.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"The {description} file was not found.",
                filePath);
        }
    }

    private static int? ParseNullableInt(
        string? value)
    {
        return int.TryParse(
            value,
            out var parsedValue)
                ? parsedValue
                : null;
    }

    private static string GetOrdinal(
        bool ordinalSpecified,
        string? ordinal)
    {
        if (!ordinalSpecified ||
            string.IsNullOrWhiteSpace(
                ordinal))
        {
            return UnspecifiedOrdinal;
        }

        return ordinal;
    }

    private static string NormalizeRequiredValue(
        string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
            value)
                ? null
                : value.Trim();
    }

    private readonly record struct RelationshipKey(
        int CweId,
        int RelatedCweId,
        string Nature,
        int? ViewId,
        bool IsDerived);

    private readonly record struct SemanticRuleKey(
        int SourceCweId,
        int TargetCweId,
        string Relationship,
        string? ScannerRuleId,
        string? ProgrammingLanguage,
        int Version);

    private readonly record struct ToolSeed(
        string Name,
        string Format);
}