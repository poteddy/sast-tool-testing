using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.CweRelationshipEngine;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class ReportingService : IReportingService,IDisposable
{
    private readonly ILogger<ReportingService> _logger;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    private readonly ICweRelationshipService
        _relationshipService;
    private bool disposedValue;
    private ICweTopologyService _cweTopologyService;
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
        _cweTopologyService = cweTopologyService;
    }

    public async Task<StringBuilder> GenerateReport(
        int scanid,
        int toolid)
    {
        return await GenerateReportAsync(
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
            var testResults = await context.CWETestResults
                .AsNoTracking()
                .Where(result => result.ScanId == scanId)
                .Select(result => new TestResultInput(
                    result.ScannerFoundCWE,
                    result.TestPathListedCWE))
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
             * Evaluate each distinct CWE pair only once.
             *
             * If 300 findings contain CWE-676 -> CWE-121,
             * the relationship engine only evaluates the pair once.
             */
            var groupedPairs = testResults
                .GroupBy(result => new CwePair(
                    result.ScannerCweId,
                    result.GroundTruthCweId))
                .Select(group => new
                {
                    Pair = group.Key,
                    Count = group.Count()
                })
                .ToList();

            var reports = new List<Report>(
                groupedPairs.Count);

            var reportSummary = new StringBuilder();

            reportSummary.AppendLine(
                $"CWE report for scan {scanId}, tool {toolId}");

            reportSummary.AppendLine(
                $"Total findings: {testResults.Count}");

            reportSummary.AppendLine(
                $"Distinct CWE pairs: {groupedPairs.Count}");

            reportSummary.AppendLine();

            foreach (var groupedPair in groupedPairs)
            {
                var relationship =
                    await _relationshipService.EvaluateAsync(
                        scannerCweId:
                            groupedPair.Pair.ScannerCweId,
                        groundTruthCweId:
                            groupedPair.Pair.GroundTruthCweId,
                        scannerRuleId: null,
                        programmingLanguage: null,
                        cancellationToken);
                CweTopologyMatch topology = null;
                try
                {
                    topology = await _cweTopologyService.GetRelationshipAsync(groupedPair.Pair.GroundTruthCweId, groupedPair.Pair.ScannerCweId, CancellationToken.None);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                /*
                 * Decide whether unrelated pairs should be persisted.
                 *
                 * Keeping them is useful for calculating false-positive
                 * counts. Remove this block if you only want matched
                 * relationships in the Reports table.
                 */
                reports.Add(
                    CreateReportEntity(
                        scanId,
                        toolId,
                        groupedPair.Pair,
                        groupedPair.Count,
                        relationship,
                        topology));

                AppendSummaryLine(
                    reportSummary,
                    groupedPair.Pair,
                    groupedPair.Count,
                    relationship);

                _logger.LogInformation(
                    "Scan {ScanId}: scanner CWE-{ScannerCweId} " +
                    "to ground-truth CWE-{GroundTruthCweId} is " +
                    "{Relationship}, score {Score}, count {Count}.",
                    scanId,
                    groupedPair.Pair.ScannerCweId,
                    groupedPair.Pair.GroundTruthCweId,
                    relationship.Relationship,
                    relationship.Score,
                    groupedPair.Count);
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

    private static Report CreateReportEntity(
       int scanId,
       int toolId,
       CwePair pair,
       int count,
       RelationshipResult relationship,
       CweTopologyMatch topology)
    {
        return new Report
        {
            ScanId = scanId,
            ToolId = toolId,

            CweId = pair.GroundTruthCweId,
            RelatedId = pair.ScannerCweId,

            Count = count,

            Relationship = relationship.Relationship.ToString(),
            RelationshipScore = relationship.Score,
            TopologyRelationship =
                topology.Relationship.ToString(),

            GroundTruthAbstraction =
                topology.SourceAbstraction.ToString(),

            ScannerAbstraction =
                topology.TargetAbstraction.ToString(),

            TopologyDistance =
                topology.Distance
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
         * Delete old generated results so the operation is
         * idempotent.
         *
         * ExecuteDeleteAsync requires a relational provider.
         * Use RemoveRange for the EF InMemory provider.
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
        CwePair pair,
        int count,
        RelationshipResult relationship)
    {
        summary.Append("CWE-");
        summary.Append(pair.ScannerCweId);
        summary.Append(" -> CWE-");
        summary.Append(pair.GroundTruthCweId);
        summary.Append(": ");
        summary.Append(relationship.Relationship);
        summary.Append(", score ");
        summary.Append(relationship.Score);
        summary.Append(", classification ");
        summary.Append(relationship.Classification);
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
        int GroundTruthCweId);

    private readonly record struct CwePair(
        int ScannerCweId,
        int GroundTruthCweId);

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ReportingService()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}