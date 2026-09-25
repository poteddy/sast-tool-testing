using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Application.CweRelationshipEngine;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Extensions;

namespace ToolTester.Infrastructure.Persistance;

public sealed class ApplicationDbContextSeed
    : IApplicationDbContextSeed
{
    private const string MitreSource = "MITRE";
    private const string DerivedInverseSource = "DerivedInverse";
    private const string UnspecifiedOrdinal = "Unspecified";

    private readonly IConfiguration _configuration;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    private readonly IZipfileService _zipfileService;

    public ApplicationDbContextSeed(
        IConfiguration configuration,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IZipfileService zipfileService)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(nameof(contextFactory));

        _zipfileService = zipfileService
            ?? throw new ArgumentNullException(nameof(zipfileService));
    }

    public async Task SeedCWECatalog()
    {
        await SeedCweCatalogAsync(CancellationToken.None);
    }

    private async Task SeedCweCatalogAsync(
        CancellationToken cancellationToken)
    {
        var semanticRulesFilePath = GetRulesFilePath();

        var semanticRules =
            await LoadSemanticRulesAsync(
                semanticRulesFilePath,
                cancellationToken);

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

    private static string GetRulesFilePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments),
            "ToolTester");

        Directory.CreateDirectory(folder);

        return Path.Combine(
            folder,
            "semantic-rules.json");
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
        await UpsertCweNodesAsync(
            context,
            weaknessCatalog,
            cancellationToken);

        await UpsertDirectRelationshipsAsync(
            context,
            weaknessCatalog,
            cancellationToken);

        await AddInverseRelationshipsAsync(
            context,
            cancellationToken);

        await UpsertSemanticRulesAsync(
            context,
            semanticRules,
            cancellationToken);
    }

    private static async Task UpsertCweNodesAsync(
        ApplicationDbContext context,
        Weakness_Catalog catalog,
        CancellationToken cancellationToken)
    {
        var existingByCweId = await context.CWECatalogs
            .ToDictionaryAsync(
                x => x.CweId,
                cancellationToken);

        foreach (var weakness in catalog.Weaknesses)
        {
            if (!int.TryParse(
                    weakness.ID,
                    out var cweId))
            {
                continue;
            }

            var abstraction = CweAbstractionParser.Parse(weakness.Abstraction.ToString()).ToString();

            var status =
                weakness.Status.ToString();

            if (existingByCweId.TryGetValue(
                    cweId,
                    out var existing))
            {
                existing.Name = weakness.Name;
                existing.Description = weakness.Description;
                existing.Abstraction = abstraction;
                existing.Status = status;

                continue;
            }

            var cwe = new CWECatalog
            {
                CweId = cweId,
                Name = weakness.Name,
                Description = weakness.Description,
                Abstraction = abstraction,
                Status = status
            };

            context.CWECatalogs.Add(cwe);
            existingByCweId.Add(cweId, cwe);
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
        var validCweIds = await context.CWECatalogs
            .AsNoTracking()
            .Select(x => x.CweId)
            .ToHashSetAsync(cancellationToken);

        var existingRelationshipData =
            await context.Relationships
                .AsNoTracking()
                .Where(x => !x.IsDerived)
                .Select(x => new
                {
                    x.CweId,
                    x.RelatedCweID,
                    x.Nature,
                    x.ViewId,
                    x.IsDerived
                })
                .ToListAsync(cancellationToken);

        var existingKeys = existingRelationshipData
            .Select(x => new RelationshipKey(
                x.CweId,
                x.RelatedCweID,
                x.Nature,
                x.ViewId,
                x.IsDerived))
            .ToHashSet();

        foreach (var weakness in catalog.Weaknesses)
        {
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

            foreach (var related in weakness.Related_Weaknesses)
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

                var key = new RelationshipKey(
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
                        Ordinal = GetOrdinal(
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
                .Where(x => !x.IsDerived)
                .ToListAsync(cancellationToken);

        var existingRelationshipData =
            await context.Relationships
                .AsNoTracking()
                .Select(x => new
                {
                    x.CweId,
                    x.RelatedCweID,
                    x.Nature,
                    x.ViewId,
                    x.IsDerived
                })
                .ToListAsync(cancellationToken);

        var existingKeys = existingRelationshipData
            .Select(x => new RelationshipKey(
                x.CweId,
                x.RelatedCweID,
                x.Nature,
                x.ViewId,
                x.IsDerived))
            .ToHashSet();

        foreach (var relationship in directRelationships)
        {
            var inverseNature =
                GetInverseNature(
                    relationship.Nature);

            if (inverseNature is null)
            {
                continue;
            }

            var key = new RelationshipKey(
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
                    CweId = relationship.RelatedCweID,
                    RelatedCweID = relationship.CweId,
                    Nature = inverseNature,
                    ViewId = relationship.ViewId,
                    ChainId = relationship.ChainId,
                    OrderSpecified =
                        relationship.OrderSpecified,
                    Ordinal =
                        relationship.Ordinal ??
                        UnspecifiedOrdinal,
                    IsDerived = true,
                    Distance = 1,
                    Source = DerivedInverseSource
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

        var validCweIds = await context.CWECatalogs
            .AsNoTracking()
            .Select(x => x.CweId)
            .ToHashSetAsync(cancellationToken);

        var existingRules = await context.CweSemanticRules
            .ToListAsync(cancellationToken);

        var existingByKey = existingRules
            .GroupBy(CreateSemanticRuleKey)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var importedKeys =
            new HashSet<SemanticRuleKey>();

        foreach (var rule in rules)
        {
            ValidateSemanticRule(
                rule,
                validCweIds);

            var key = CreateSemanticRuleKey(rule);

            if (!importedKeys.Add(key))
            {
                throw new InvalidDataException(
                    $"The semantic-rule JSON contains a duplicate " +
                    $"mapping for CWE-{rule.SourceCweId} to " +
                    $"CWE-{rule.TargetCweId}, relationship " +
                    $"'{rule.Relationship}', version {rule.Version}.");
            }

            if (existingByKey.TryGetValue(
                    key,
                    out var existing))
            {
                UpdateSemanticRule(
                    existing,
                    rule);

                continue;
            }

            var entity = CreateSemanticRuleEntity(rule);

            context.CweSemanticRules.Add(entity);
            existingByKey.Add(key, entity);
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static CweSemanticRule CreateSemanticRuleEntity(
        SemanticRule rule)
    {
        return new CweSemanticRule
        {
            SourceCweId = rule.SourceCweId,
            TargetCweId = rule.TargetCweId,
            Relationship = rule.Relationship.ToString(),
            Score = rule.Score,
            ScannerRuleId =
                NormalizeOptionalValue(
                    rule.ScannerRuleId),
            ProgrammingLanguage =
                NormalizeOptionalValue(
                    rule.ProgrammingLanguage),
            Rationale = rule.Rationale.Trim(),
            EvidenceReference =
                rule.EvidenceReference.Trim(),
            Bidirectional = rule.Bidirectional,
            Enabled = rule.Enabled,
            Version = rule.Version
        };
    }

    private static void UpdateSemanticRule(
        CweSemanticRule entity,
        SemanticRule import)
    {
        entity.Score = import.Score;
        entity.Rationale = import.Rationale.Trim();
        entity.EvidenceReference =
            import.EvidenceReference.Trim();
        entity.Bidirectional = import.Bidirectional;
        entity.Enabled = import.Enabled;

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
            rule.SourceCweId,
            rule.TargetCweId,
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
            rule.SourceCweId,
            rule.TargetCweId,
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
        if (rule.SourceCweId <= 0)
        {
            throw new InvalidDataException(
                "Semantic rule SourceCweId must be greater than zero.");
        }

        if (!validCweIds.Contains(rule.SourceCweId))
        {
            throw new InvalidDataException(
                $"Semantic rule source CWE-{rule.SourceCweId} " +
                "does not exist in the imported MITRE catalog.");
        }

        if (rule.TargetCweId <= 0)
        {
            throw new InvalidDataException(
                "Semantic rule TargetCweId must be greater than zero.");
        }

        if (!validCweIds.Contains(rule.TargetCweId))
        {
            throw new InvalidDataException(
                $"Semantic rule target CWE-{rule.TargetCweId} " +
                "does not exist in the imported MITRE catalog.");
        }

        if (string.IsNullOrWhiteSpace(
                rule.Relationship.ToString()))
        {
            throw new InvalidDataException(
                "Semantic rule Relationship is required.");
        }

        if (!Enum.TryParse<CweRelationshipKind>(
                rule.Relationship.ToString(),
                ignoreCase: false,
                out _))
        {
            throw new InvalidDataException(
                $"Unknown semantic relationship " +
                $"'{rule.Relationship}' for " +
                $"CWE-{rule.SourceCweId} to " +
                $"CWE-{rule.TargetCweId}.");
        }

        if (rule.Score is < 0 or > 1000)
        {
            throw new InvalidDataException(
                $"Semantic-rule score must be between " +
                $"0 and 1000. Received {rule.Score} for " +
                $"CWE-{rule.SourceCweId} to " +
                $"CWE-{rule.TargetCweId}.");
        }

        if (rule.Version <= 0)
        {
            throw new InvalidDataException(
                "Semantic rule Version must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(rule.Rationale))
        {
            throw new InvalidDataException(
                $"Semantic-rule rationale is required for " +
                $"CWE-{rule.SourceCweId} to " +
                $"CWE-{rule.TargetCweId}.");
        }

        if (string.IsNullOrWhiteSpace(
                rule.EvidenceReference))
        {
            throw new InvalidDataException(
                $"Semantic-rule evidence reference is required " +
                $"for CWE-{rule.SourceCweId} to " +
                $"CWE-{rule.TargetCweId}.");
        }
    }

    private static async Task<
        IReadOnlyCollection<SemanticRule>>
        LoadSemanticRulesAsync(
            string filePath,
            CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream =
            File.OpenRead(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling =
                JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        try
        {
            var rules =
                await JsonSerializer.DeserializeAsync<
                List<SemanticRule>>(stream, options,cancellationToken);

            return rules ?? [];
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"The semantic-rule file '{filePath}' " +
                "contains invalid JSON.",
                exception);
        }
    }

    private async Task SeedJulietCoverageAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (await context.JulietCoverages
                .AnyAsync(cancellationToken))
        {
            return;
        }

        var settings = _configuration
            .GetRequiredSection("JulietProjectSetting")
            .Get<JulietProjectSetting>()
            ?? throw new InvalidOperationException(
                "JulietProjectSetting is missing.");

        if (string.IsNullOrWhiteSpace(settings.Path))
        {
            throw new InvalidOperationException(
                "The Juliet project path is empty.");
        }

        var fileCounts =
            _zipfileService.FileCount(settings.Path);

        foreach (var fileCount in fileCounts)
        {
            context.JulietCoverages.Add(
                new JulietCoverage
                {
                    CweId = fileCount.Key,
                    Covered = fileCount.Value
                });
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(
                cancellationToken);
        }
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
                Format: "Cpp Checker")

        };

        var existingToolNames =
            await context.Tools
                .AsNoTracking()
                .Select(x => x.Name)
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
                    Name = desiredTool.Name,
                    Format = desiredTool.Format
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
        if (string.IsNullOrWhiteSpace(filePath))
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
            string.IsNullOrWhiteSpace(ordinal))
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
        return string.IsNullOrWhiteSpace(value)
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