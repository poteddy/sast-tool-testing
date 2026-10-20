using Microsoft.EntityFrameworkCore;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Reports.DTO;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class UnrelatedFindingConsensusService
    : IUnrelatedFindingConsensusService
{
    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    public UnrelatedFindingConsensusService(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
    }

    public async Task<IReadOnlyList<UnrelatedFindingConsensus>>
        GetConsensusAsync(
            int minimumScannerCount = 2,
            CancellationToken cancellationToken = default)
    {
        if (minimumScannerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumScannerCount),
                minimumScannerCount,
                "Minimum scanner count must be greater than zero.");
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        var unrelatedRelationship =
            CweRelationshipKind.Unrelated.ToString();

        /*
         * ReportingService is authoritative for relationship
         * classification.
         *
         * Do not recalculate the CWE relationship here.
         */
        var rows =
            await (
                from report in context.Reports.AsNoTracking()

                join scan in context.Scans.AsNoTracking()
                    on report.ScanId equals scan.Id

                join tool in context.Tools.AsNoTracking()
                    on scan.ToolId equals tool.Id

                where
                    report.Relationship ==
                        unrelatedRelationship
                    &&
                    report.ScannerCweId.HasValue

                select new UnrelatedReportRow(
                    report.ScanId,
                    scan.ToolId,
                    tool.Name,
                    report.ScannerCweId!.Value,
                    report.GroundTruthCweId,
                    report.RootCauseCweId,
                    report.Count))
                .ToListAsync(
                    cancellationToken);

        /*
         * Build consensus from the persisted Report rows.
         *
         * ScannerCount represents distinct scanner products,
         * not the number of scans.
         */
        var consensus =
            rows
                .GroupBy(row => new
                {
                    row.ScannerCweId,
                    row.GroundTruthCweId
                })
                .Select(group =>
                {
                    var scannerIds =
                        group
                            .Select(row =>
                                row.ToolId)
                            .Distinct()
                            .OrderBy(id =>
                                id)
                            .ToArray();

                    var scannerNames =
                        group
                            .GroupBy(row =>
                                row.ToolId)
                            .Select(toolGroup =>
                                toolGroup
                                    .Select(row =>
                                        row.ToolName)
                                    .First())
                            .OrderBy(name =>
                                name)
                            .ToArray();

                    var scanIds =
                        group
                            .Select(row =>
                                row.ScanId)
                            .Distinct()
                            .OrderBy(id =>
                                id)
                            .ToArray();

                    return new UnrelatedFindingConsensus
                    {
                        ScannerFoundCweId =
                            group.Key.ScannerCweId,

                        GroundTruthCweId =
                            group.Key.GroundTruthCweId,

                        ScannerCount =
                            scannerIds.Length,

                        ScanCount =
                            scanIds.Length,

                        FindingCount =
                            group.Sum(row =>
                                row.Count),

                        ScannerIds =
                            scannerIds,

                        ScannerNames =
                            scannerNames,

                        ScanIds =
                            scanIds
                    };
                })
                .Where(item =>
                    item.ScannerCount >=
                    minimumScannerCount)
                .OrderByDescending(item =>
                    item.ScannerCount)
                .ThenByDescending(item =>
                    item.FindingCount)
                .ThenBy(item =>
                    item.GroundTruthCweId)
                .ThenBy(item =>
                    item.ScannerFoundCweId)
                .ToList();

        if (consensus.Count == 0)
        {
            return consensus;
        }

        /*
         * Retrieve raw scanner findings so that each consensus
         * pair can expose one representative finding per scanner.
         *
         * This is diagnostic information only. It does not
         * participate in determining consensus.
         */

        var relevantScanIds =
            consensus
                .SelectMany(item =>
                    item.ScanIds)
                .Distinct()
                .ToArray();

        var relevantScannerCweIds =
            consensus
                .Select(item =>
                    item.ScannerFoundCweId)
                .Distinct()
                .ToArray();

        var relevantGroundTruthCweIds =
            consensus
                .Select(item =>
                    item.GroundTruthCweId)
                .Distinct()
                .ToArray();

        var rawFindings =
            await context.CWETestResults
                .AsNoTracking()
                .Where(result =>
                    relevantScanIds.Contains(
                        result.ScanId)
                    &&
                    result.ScannerFoundCWE.HasValue
                    &&
                    relevantScannerCweIds.Contains(
                        result.ScannerFoundCWE.Value)
                    &&
                    relevantGroundTruthCweIds.Contains(
                        result.Test.CweId))
                .Select(result =>
                    new RawConsensusFinding(
                        result.ScanId,

                        result.Scan.ToolId,

                        result.Scan.Tool.Name,

                        result.TestId,

                        result.Test.CweId,

                        result.ScannerFoundCWE!.Value,

                        result.FilePath,

                        result.Line,

                        result.VulnIdFromTool,

                        result.Title))
                .ToListAsync(
                    cancellationToken);

        /*
         * Attach one representative raw finding from each
         * distinct scanner to every consensus pair.
         */
        foreach (var item in consensus)
        {
            var examples =
                rawFindings
                    .Where(finding =>
                        finding.ScannerCweId ==
                            item.ScannerFoundCweId
                        &&
                        finding.GroundTruthCweId ==
                            item.GroundTruthCweId
                        &&
                        item.ScannerIds.Contains(
                            finding.ToolId))
                    .GroupBy(finding =>
                        finding.ToolId)
                    .Select(scannerGroup =>
                    {
                        /*
                         * Pick a deterministic representative
                         * finding for this scanner.
                         *
                         * Prefer findings that contain a file path,
                         * then a line number, then a tool rule ID.
                         */
                        var finding =
                            scannerGroup
                                .OrderByDescending(candidate =>
                                    !string.IsNullOrWhiteSpace(
                                        candidate.FilePath))
                                .ThenByDescending(candidate =>
                                    candidate.Line.HasValue)
                                .ThenByDescending(candidate =>
                                    !string.IsNullOrWhiteSpace(
                                        candidate.RuleId))
                                .ThenBy(candidate =>
                                    candidate.FilePath)
                                .ThenBy(candidate =>
                                    candidate.Line)
                                .ThenBy(candidate =>
                                    candidate.TestId)
                                .First();

                        return new ConsensusFindingExample
                        {
                            ToolId =
                                finding.ToolId,

                            ScannerName =
                                finding.ToolName,

                            ScanId =
                                finding.ScanId,

                            TestId =
                                finding.TestId,

                            FilePath =
                                finding.FilePath,

                            Line =
                                finding.Line,

                            RuleId =
                                finding.RuleId
                                ?? string.Empty,

                            Title =
                                finding.Title
                                ?? string.Empty
                        };
                    })
                    .OrderBy(example =>
                        example.ScannerName)
                    .ToArray();

            item.Examples = examples;
        }

        return consensus;
    }

    public async Task<IReadOnlyList<UnrelatedFindingConsensus>>
        GetConsensusForGroundTruthCweAsync(
            int groundTruthCweId,
            int minimumScannerCount = 2,
            CancellationToken cancellationToken = default)
    {
        if (groundTruthCweId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(groundTruthCweId),
                groundTruthCweId,
                "Ground-truth CWE must be greater than zero.");
        }

        var consensus =
            await GetConsensusAsync(
                minimumScannerCount,
                cancellationToken);

        return consensus
            .Where(item =>
                item.GroundTruthCweId ==
                groundTruthCweId)
            .ToList();
    }

    public async Task<IReadOnlyList<UnrelatedFindingConsensus>>
        GetAllUnrelatedAsync(
            CancellationToken cancellationToken = default)
    {
        /*
         * MinimumScannerCount = 1 returns every unrelated
         * Scanner CWE / Ground Truth CWE pair, including
         * findings reported by only one scanner.
         */
        return await GetConsensusAsync(
            minimumScannerCount: 1,
            cancellationToken);
    }

    private sealed record UnrelatedReportRow(
        int ScanId,
        int ToolId,
        string ToolName,
        int ScannerCweId,
        int GroundTruthCweId,
        int? RootCauseCweId,
        int Count);

    private sealed record RawConsensusFinding(
        int ScanId,
        int ToolId,
        string ToolName,
        int TestId,
        int GroundTruthCweId,
        int ScannerCweId,
        string? FilePath,
        int? Line,
        string? RuleId,
        string? Title);
}