using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.CweRelationshipEngine;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class ReportingService : IReportingService, IDisposable
{
    private readonly ILogger<ReportingService> _logger;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    private readonly ICweRelationshipService
        _relationshipService;

    private readonly ICweTopologyService
        _cweTopologyService;

    private bool _disposedValue;

    public ReportingService(
        ILogger<ReportingService> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICweRelationshipService relationshipService,
        ICweTopologyService cweTopologyService)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(nameof(contextFactory));

        _relationshipService = relationshipService
            ?? throw new ArgumentNullException(
                nameof(relationshipService));

        _cweTopologyService = cweTopologyService
            ?? throw new ArgumentNullException(
                nameof(cweTopologyService));
    }

    public Task<StringBuilder> GenerateReport(
        int scanid,
        int toolid)
    {
        return GenerateReportAsync(
            scanid,
            toolid,
            CancellationToken.None);
    }

    private async Task<StringBuilder> GenerateReportAsync(
        int scanId,
        int toolId,
        CancellationToken cancellationToken)
    {
        ValidateArguments(scanId, toolId);

        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        try
        {
            /*
             * Preserve all three different CWE values:
             *
             * ScannerCweId:
             *     CWE reported by the scanner.
             *
             * GroundTruthCweId:
             *     CWE identified by the Juliet test path.
             *
             * RootCauseCweId:
             *     Root cause selected by the semantic resolver.
             */
            var testResults = await context.CWETestResults
                .AsNoTracking()
                .Where(result => result.ScanId == scanId)
                .Select(result => new TestResultInput(
                    result.ScannerFoundCWE,
                    result.TestPathListedCWE,
                    result.RootCauseCWE))
                .Where(result =>
                    result.ScannerCweId > 0 &&
                    result.GroundTruthCweId > 0)
                .ToListAsync(cancellationToken);

            if (testResults.Count == 0)
            {
                _logger.LogInformation(
                    "No CWE test results were found for scan {ScanId}.",
                    scanId);

                return CreateEmptyReport(scanId, toolId);
            }

            /*
             * Root cause is included in the grouping key.
             *
             * This prevents these from being combined:
             *
             * Scanner CWE-676 -> ground truth CWE-121
             *     -> root cause CWE-119
             *
             * Scanner CWE-676 -> ground truth CWE-121
             *     -> no identified root cause
             */
            var groupedResults = testResults
                .GroupBy(result => new CweReportKey(
                    result.ScannerCweId,
                    result.GroundTruthCweId,
                    result.RootCauseCweId))
                .Select(group => new GroupedCweResult(
                    group.Key,
                    group.Count()))
                .ToList();

            var reports = new List<Report>(
                groupedResults.Count);

            var reportSummary = CreateReportHeader(
                scanId,
                toolId,
                testResults,
                groupedResults.Count);

            foreach (var groupedResult in groupedResults)
            {
                var key = groupedResult.Key;

                var relationship =
                    await _relationshipService.EvaluateAsync(
                        scannerCweId: key.ScannerCweId,
                        groundTruthCweId: key.GroundTruthCweId,
                        scannerRuleId: null,
                        programmingLanguage: null,
                        cancellationToken);

                var topology = await TryGetTopologyAsync(
                    key.GroundTruthCweId,
                    key.ScannerCweId,
                    cancellationToken);

                reports.Add(
                    CreateReportEntity(
                        scanId,
                        toolId,
                        key,
                        groupedResult.Count,
                        relationship,
                        topology));

                AppendSummaryLine(
                    reportSummary,
                    key,
                    groupedResult.Count,
                    relationship,
                    topology);

                _logger.LogInformation(
                    "Scan {ScanId}: scanner CWE-{ScannerCweId}, " +
                    "ground-truth CWE-{GroundTruthCweId}, " +
                    "root-cause CWE-{RootCauseCweId}, " +
                    "relationship {Relationship}, score {Score}, " +
                    "topology {TopologyRelationship}, count {Count}.",
                    scanId,
                    key.ScannerCweId,
                    key.GroundTruthCweId,
                    key.RootCauseCweId,
                    relationship.Relationship,
                    relationship.Score,
                    topology?.Relationship,
                    groupedResult.Count);
            }

            await ReplaceReportsAsync(
                context,
                scanId,
                toolId,
                reports,
                cancellationToken);

            return reportSummary;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate the CWE report for " +
                "scan {ScanId} and tool {ToolId}.",
                scanId,
                toolId);

            throw;
        }
    }

    private async Task<CweTopologyMatch?> TryGetTopologyAsync(
        int groundTruthCweId,
        int scannerCweId,
        CancellationToken cancellationToken)
    {
        try
        {
            /*
             * Source is the ground-truth CWE.
             * Target is the scanner-reported CWE.
             */
            return await _cweTopologyService.GetRelationshipAsync(
                groundTruthCweId,
                scannerCweId,
                cancellationToken);
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(
                exception,
                "Topology could not be calculated between " +
                "ground-truth CWE-{GroundTruthCweId} and " +
                "scanner CWE-{ScannerCweId}.",
                groundTruthCweId,
                scannerCweId);

            return null;
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "No topology relationship was available between " +
                "ground-truth CWE-{GroundTruthCweId} and " +
                "scanner CWE-{ScannerCweId}.",
                groundTruthCweId,
                scannerCweId);

            return null;
        }
    }

    private static StringBuilder CreateReportHeader(
        int scanId,
        int toolId,
        IReadOnlyCollection<TestResultInput> testResults,
        int distinctCombinationCount)
    {
        var rootCauseIdentifiedCount = testResults.Count(
            result => result.RootCauseCweId.HasValue);

        var rootCauseMissingCount =
            testResults.Count - rootCauseIdentifiedCount;

        var rootCauseMatchesGroundTruthCount = testResults.Count(
            result =>
                result.RootCauseCweId.HasValue &&
                result.RootCauseCweId.Value ==
                result.GroundTruthCweId);

        var rootCauseMatchesScannerCount = testResults.Count(
            result =>
                result.RootCauseCweId.HasValue &&
                result.RootCauseCweId.Value ==
                result.ScannerCweId);

        var summary = new StringBuilder();

        summary.AppendLine(
            $"CWE report for scan {scanId}, tool {toolId}");

        summary.AppendLine(
            $"Total findings: {testResults.Count}");

        summary.AppendLine(
            $"Distinct CWE combinations: {distinctCombinationCount}");

        summary.AppendLine(
            $"Findings with identified root cause: " +
            $"{rootCauseIdentifiedCount}");

        summary.AppendLine(
            $"Findings without identified root cause: " +
            $"{rootCauseMissingCount}");

        summary.AppendLine(
            $"Root cause matches ground truth: " +
            $"{rootCauseMatchesGroundTruthCount}");

        summary.AppendLine(
            $"Root cause matches scanner CWE: " +
            $"{rootCauseMatchesScannerCount}");

        summary.AppendLine();

        return summary;
    }

    private static Report CreateReportEntity(
        int scanId,
        int toolId,
        CweReportKey key,
        int count,
        RelationshipResult relationship,
        CweTopologyMatch? topology)
    {
        return new Report
        {
            ScanId = scanId,
            ToolId = toolId,

            GroundTruthCweId = key.GroundTruthCweId,
            ScannerCweId = key.ScannerCweId,
            RootCauseCweId = key.RootCauseCweId,

            RootCauseMatchesGroundTruth =
                key.RootCauseCweId.HasValue &&
                key.RootCauseCweId.Value ==
                key.GroundTruthCweId,

            RootCauseMatchesScanner =
                key.RootCauseCweId.HasValue &&
                key.RootCauseCweId.Value ==
                key.ScannerCweId,

            Count = count,

            Relationship =
                relationship.Relationship.ToString(),

            RelationshipScore =
                relationship.Score,

            /*
             * Topology can be null when one of the CWEs is not
             * present in the imported MITRE dataset.
             */
            TopologyRelationship =
                topology?.Relationship.ToString()
                ?? "NotAvailable",

            GroundTruthAbstraction =
                topology?.SourceAbstraction.ToString()
                ?? "Unknown",

            ScannerAbstraction =
                topology?.TargetAbstraction.ToString()
                ?? "Unknown",

            TopologyDistance =
                topology?.Distance
        };
    }

    private static async Task ReplaceReportsAsync(
        ApplicationDbContext context,
        int scanId,
        int toolId,
        IReadOnlyCollection<Report> reports,
        CancellationToken cancellationToken)
    {
        /*
         * Delete previously generated reports for this scan and tool
         * so report generation remains idempotent.
         */
        if (context.Database.IsInMemory())
        {
            var existingReports = await context.Reports
                .Where(report =>
                    report.ScanId == scanId &&
                    report.ToolId == toolId)
                .ToListAsync(cancellationToken);

            context.Reports.RemoveRange(existingReports);
        }
        else
        {
            await context.Reports
                .Where(report =>
                    report.ScanId == scanId &&
                    report.ToolId == toolId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        context.Reports.AddRange(reports);

        await context.SaveChangesAsync(
            cancellationToken);
    }

    private static void AppendSummaryLine(
        StringBuilder summary,
        CweReportKey key,
        int count,
        RelationshipResult relationship,
        CweTopologyMatch? topology)
    {
        summary.Append("Scanner CWE-");
        summary.Append(key.ScannerCweId);

        summary.Append(" -> Ground truth CWE-");
        summary.Append(key.GroundTruthCweId);

        summary.Append(" -> Root cause ");

        if (key.RootCauseCweId.HasValue)
        {
            summary.Append("CWE-");
            summary.Append(key.RootCauseCweId.Value);
        }
        else
        {
            summary.Append("not identified");
        }

        summary.Append(": relationship ");
        summary.Append(relationship.Relationship);

        summary.Append(", score ");
        summary.Append(relationship.Score);

        summary.Append(", classification ");
        summary.Append(relationship.Classification);

        summary.Append(", topology ");
        summary.Append(
            topology?.Relationship.ToString()
            ?? "not available");

        summary.Append(", topology distance ");

        if (topology is not null)
        {
            summary.Append(topology.Distance);
        }
        else
        {
            summary.Append("not available");
        }

        summary.Append(", findings ");
        summary.AppendLine(count.ToString());
    }

    private static StringBuilder CreateEmptyReport(
        int scanId,
        int toolId)
    {
        var report = new StringBuilder();

        report.AppendLine(
            $"CWE report for scan {scanId}, tool {toolId}");

        report.AppendLine(
            "No CWE test results were found.");

        return report;
    }

    private static void ValidateArguments(
        int scanId,
        int toolId)
    {
        if (scanId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scanId),
                scanId,
                "Scan ID must be greater than zero.");
        }

        if (toolId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(toolId),
                toolId,
                "Tool ID must be greater than zero.");
        }
    }

    private readonly record struct TestResultInput(
        int ScannerCweId,
        int GroundTruthCweId,
        int? RootCauseCweId);

    private readonly record struct CweReportKey(
        int ScannerCweId,
        int GroundTruthCweId,
        int? RootCauseCweId);

    private readonly record struct GroupedCweResult(
        CweReportKey Key,
        int Count);

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}