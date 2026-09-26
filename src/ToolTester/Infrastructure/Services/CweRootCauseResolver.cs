using Microsoft.EntityFrameworkCore;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class CweRootCauseResolver : ICweRootCauseResolver
{
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
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        var rootCauseRule =
            await context.CweSemanticRules
                .AsNoTracking()
                .Where(rule =>
                    rule.Enabled &&
                    rule.SourceCweId == scannerCweId &&
                    IsRootCauseRelationship(
                        rule.Relationship))
                .OrderByDescending(rule => rule.Score)
                .FirstOrDefaultAsync(cancellationToken);

        if (rootCauseRule != null)
        {
            return rootCauseRule.TargetCweId;
        }

        /*
         * Exact scanner match.
         */
        if (groundTruthCweId.HasValue &&
            scannerCweId == groundTruthCweId.Value)
        {
            return scannerCweId;
        }

        return null;
    }

    private static bool IsRootCauseRelationship(
       string relationship)
    {
        return relationship ==
            nameof(CweRelationshipKind.SameRootCauseBroaderCwe)
            ||
            relationship ==
            nameof(CweRelationshipKind.SameRootCauseNarrowerCwe);
    }
}