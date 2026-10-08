using Microsoft.EntityFrameworkCore;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class CweRootCauseResolver : ICweRootCauseResolver
{
    private static readonly string[] RootCauseRelationships =
    [
        nameof(
            CweRelationshipKind.SameRootCauseBroaderCwe),

        nameof(
            CweRelationshipKind.SameRootCauseNarrowerCwe),

        nameof(
            CweRelationshipKind.JulietRootCause)
    ];

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    public CweRootCauseResolver(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
    }

    public async Task<int?> ResolveMatchedTargetCweAsync(
     int scannerCweId,
     int? groundTruthCweId = null,
     CancellationToken cancellationToken = default)
    {
        if (scannerCweId <= 0)
        {
            //throw new ArgumentOutOfRangeException(
            //    nameof(scannerCweId),
            //    scannerCweId,
            //    "The scanner CWE ID must be greater than zero.");
            return null;
        }

        if (!groundTruthCweId.HasValue)
        {
            return null;
        }

        // Exact match
        if (scannerCweId == groundTruthCweId.Value)
        {
            return groundTruthCweId.Value;
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CweSemanticRules
            .AsNoTracking()
            .Where(rule =>
                rule.Enabled &&
                rule.ScannerFoundCweId == scannerCweId &&
                rule.TestTargetCwe == groundTruthCweId.Value &&
                RootCauseRelationships.Contains(rule.Relationship))
            .OrderByDescending(rule => rule.Score)
            .Select(rule => (int?)rule.TestTargetCwe)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static bool IsRootCauseRelationship(
        string? relationship)
    {
        return relationship is not null &&
            RootCauseRelationships.Contains(
                relationship,
                StringComparer.Ordinal);
    }
}