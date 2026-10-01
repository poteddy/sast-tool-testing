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

    public async Task<int?> ResolveRootCauseAsync(
        int scannerCweId,
        int? groundTruthCweId = null,
        CancellationToken cancellationToken = default)
    {
        if (scannerCweId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scannerCweId),
                scannerCweId,
                "The scanner CWE ID must be greater than zero.");
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        /*
         * Keep the entire query translatable by SQLite.
         *
         * RootCauseRelationships.Contains(rule.Relationship)
         * is translated to a SQL IN expression.
         */
        var rootCauseCweId =
            await context.CweSemanticRules
                .AsNoTracking()
                .Where(rule =>
                    rule.Enabled &&
                    rule.SourceCweId == scannerCweId &&
                    RootCauseRelationships.Contains(
                        rule.Relationship))
                .OrderByDescending(rule => rule.Score)
                .ThenBy(rule => rule.TargetCweId)
                .Select(rule => (int?)rule.TargetCweId)
                .FirstOrDefaultAsync(cancellationToken);

        if (rootCauseCweId.HasValue)
        {
            return rootCauseCweId.Value;
        }

        /*
         * If no semantic root-cause rule exists, preserve an
         * exact scanner-to-ground-truth match.
         */
        if (groundTruthCweId.HasValue &&
            scannerCweId == groundTruthCweId.Value)
        {
            return scannerCweId;
        }

        return null;
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