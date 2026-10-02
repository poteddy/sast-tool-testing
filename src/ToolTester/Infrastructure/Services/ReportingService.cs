using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Text;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.CweRelationshipEngine;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class ReportingService :
    IReportingService,
    IDisposable
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
            ?? throw new ArgumentNullException(
                nameof(logger));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));

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
        ObjectDisposedException.ThrowIf(
            _disposedValue,
            this);

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
        ValidateArguments(
            scanId,
            toolId);

        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        try
        {
            /*
             * Verify that the requested scan exists and belongs
             * to the requested tool.
             */
            var scan = await context.Scans
                .AsNoTracking()
                .Where(item => item.Id == scanId)               
                .SingleOrDefaultAsync(
                    cancellationToken);

            if (scan is null)
            {
                throw new InvalidOperationException(
                    $"Scan {scanId} does not exist.");
            }

            if (scan.ToolId != toolId)
            {
                throw new InvalidOperationException(
                    $"Scan {scanId} belongs to tool " +
                    $"{scan.ToolId}, not tool {toolId}.");
            }

            /*
             * Apply SQLite-compatible filtering before projecting
             * into the TestResultInput record.
             *
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
                .Where(result =>
                    result.ScanId == scanId &&
                    result.ScannerFoundCWE > 0 &&
                    result.TestPathListedCWE > 0)
                .Select(result => new TestResultInput(
                    result.ScannerFoundCWE,
                    result.TestPathListedCWE,
                    result.RootCauseCWE))
                .ToListAsync(
                    cancellationToken);

            if (testResults.Count == 0)
            {
                _logger.LogInformation(
                    "No CWE test results were found for " +
                    "scan {ScanId}.",
                    scanId);

                /*
                 * Remove any old reports because the source test
                 * results are now empty.
                 */
                await ReplaceReportsAsync(
                    context,
                    scanId,
                    toolId,
                    [],
                    cancellationToken);

                return CreateEmptyReport(scan);
            }

            /*
             * Root cause is included in the grouping key.
             *
             * This prevents findings with different root-cause
             * results from being combined.
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
                cancellationToken
                    .ThrowIfCancellationRequested();

                var key =
                    groupedResult.Key;

                var relationship =
                    await _relationshipService.EvaluateAsync(
                        scannerCweId:
                            key.ScannerCweId,

                        groundTruthCweId:
                            key.GroundTruthCweId,

                        scannerRuleId:
                            null,

                        programmingLanguage:
                            null,

                        cancellationToken);

                var topology = await TryGetTopologyAsync(
                    key.GroundTruthCweId,
                    key.ScannerCweId,
                    cancellationToken);

                var report = CreateReportEntity(
                    scanId,
                    toolId,
                    key,
                    groupedResult.Count,
                    relationship,
                    topology);

                reports.Add(report);

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
                    "relationship {Relationship}, " +
                    "score {Score}, " +
                    "topology {TopologyRelationship}, " +
                    "count {Count}.",
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
            return await _cweTopologyService
                .GetRelationshipAsync(
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
        var rootCauseIdentifiedCount =
            testResults.Count(
                result =>
                    result.RootCauseCweId.HasValue);

        var rootCauseMissingCount =
            testResults.Count -
            rootCauseIdentifiedCount;

        var rootCauseMatchesGroundTruthCount =
            testResults.Count(
                result =>
                    result.RootCauseCweId.HasValue &&
                    result.RootCauseCweId.Value ==
                    result.GroundTruthCweId);

        var rootCauseMatchesScannerCount =
            testResults.Count(
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
            $"Distinct CWE combinations: " +
            $"{distinctCombinationCount}");

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
            ScanId =
                scanId,

            ToolId =
                toolId,

            /*
             * These are CWE numbers, not CWECatalog primary keys.
             *
             * They should remain scalar report values unless their
             * relationships are explicitly configured against
             * CWECatalog.CweId as an alternate principal key.
             */
            GroundTruthCweId =
                key.GroundTruthCweId,

            ScannerCweId =
                key.ScannerCweId,

            RootCauseCweId =
                key.RootCauseCweId,

            RootCauseMatchesGroundTruth =
                key.RootCauseCweId.HasValue &&
                key.RootCauseCweId.Value ==
                key.GroundTruthCweId,

            RootCauseMatchesScanner =
                key.RootCauseCweId.HasValue &&
                key.RootCauseCweId.Value ==
                key.ScannerCweId,

            Count =
                count,

            Relationship =
                relationship.Relationship.ToString(),

            RelationshipScore =
                relationship.Score,

            /*
             * Topology can be unavailable when one of the CWEs
             * does not exist in the imported MITRE dataset.
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

    private async Task ReplaceReportsAsync(
        ApplicationDbContext context,
        int scanId,
        int toolId,
        IReadOnlyCollection<Report> reports,
        CancellationToken cancellationToken)
    {
        /*
         * Validate the actual foreign-key parents before attempting
         * to replace the report rows.
         */
        var scanExists = await context.Scans
            .AsNoTracking()
            .AnyAsync(
                scan =>
                    scan.Id == scanId &&
                    scan.ToolId == toolId,
                cancellationToken);

        if (!scanExists)
        {
            throw new InvalidOperationException(
                $"Scan {scanId} for tool {toolId} " +
                "does not exist.");
        }

        var toolExists = await context.Tools
            .AsNoTracking()
            .AnyAsync(
                tool => tool.Id == toolId,
                cancellationToken);

        if (!toolExists)
        {
            throw new InvalidOperationException(
                $"Tool {toolId} does not exist.");
        }

        IDbContextTransaction? transaction = null;

        try
        {
            if (context.Database.IsRelational())
            {
                transaction =
                    await context.Database
                        .BeginTransactionAsync(
                            cancellationToken);
            }

            /*
             * Delete previously generated reports for this scan
             * and tool so report generation remains idempotent.
             */
            if (context.Database.IsInMemory())
            {
                var existingReports =
                    await context.Reports
                        .Where(report =>
                            report.ScanId == scanId &&
                            report.ToolId == toolId)
                        .ToListAsync(
                            cancellationToken);

                context.Reports.RemoveRange(
                    existingReports);

                /*
                 * Save the tracked deletions before adding the
                 * replacement records.
                 */
                if (existingReports.Count > 0)
                {
                    await context.SaveChangesAsync(
                        cancellationToken);
                }
            }
            else
            {
                await context.Reports
                    .Where(report =>
                        report.ScanId == scanId &&
                        report.ToolId == toolId)
                    .ExecuteDeleteAsync(
                        cancellationToken);
            }

            foreach (var report in reports)
            {
                /*
                 * Ensure EF treats every replacement as a new row.
                 */
                report.Id = 0;

                /*
                 * Ensure all reports reference the validated
                 * parent records.
                 */
                report.ScanId = scanId;
                report.ToolId = toolId;
            }

            if (reports.Count > 0)
            {
                context.Reports.AddRange(
                    reports);

                await context.SaveChangesAsync(
                    cancellationToken);
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }

            _logger.LogInformation(
                "Replaced reports for scan {ScanId}, " +
                "tool {ToolId}. New report count: {Count}.",
                scanId,
                toolId,
                reports.Count);
        }
        catch (DbUpdateException exception)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);
            }

            LogFailedReportEntities(
                context,
                exception);

            throw;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private void LogFailedReportEntities(
        ApplicationDbContext context,
        DbUpdateException exception)
    {
        var entries = exception.Entries.Count > 0
            ? exception.Entries
            : context.ChangeTracker
                .Entries()
                .Where(entry =>
                    entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified ||
                    entry.State == EntityState.Deleted)
                .ToList();

        foreach (var entry in entries)
        {
            _logger.LogError(
                "Failed report entity {EntityType}, " +
                "state {State}.",
                entry.Metadata.DisplayName(),
                entry.State);

            foreach (var property in entry.Properties)
            {
                _logger.LogError(
                    "Property {PropertyName}={PropertyValue}.",
                    property.Metadata.Name,
                    property.CurrentValue);
            }

            foreach (var foreignKey in
                     entry.Metadata.GetForeignKeys())
            {
                var values = foreignKey.Properties
                    .Select(property =>
                    {
                        var value = entry
                            .Property(property.Name)
                            .CurrentValue;

                        return
                            $"{property.Name}=" +
                            $"{value ?? "<null>"}";
                    });

                _logger.LogError(
                    "Foreign key from {DependentEntity} to " +
                    "{PrincipalEntity}: {Values}.",
                    entry.Metadata.DisplayName(),
                    foreignKey.PrincipalEntityType.DisplayName(),
                    string.Join(", ", values));
            }
        }

        _logger.LogError(
            exception,
            "SQLite rejected one or more report changes " +
            "because of a foreign-key constraint.");
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
            summary.Append(
                key.RootCauseCweId.Value);
        }
        else
        {
            summary.Append(
                "not identified");
        }

        summary.Append(": relationship ");
        summary.Append(
            relationship.Relationship);

        summary.Append(", score ");
        summary.Append(
            relationship.Score);

        summary.Append(", classification ");
        summary.Append(
            relationship.Classification);

        summary.Append(", topology ");
        summary.Append(
            topology?.Relationship.ToString()
            ?? "not available");

        summary.Append(", topology distance ");

        if (topology is not null)
        {
            summary.Append(
                topology.Distance);
        }
        else
        {
            summary.Append(
                "not available");
        }

        summary.Append(", findings ");
        summary.AppendLine(
            count.ToString());
    }

    private static StringBuilder CreateEmptyReport(Scan scan)
    {
        var report = new StringBuilder();

        report.AppendLine(
            $"CWE report for scan {scan.Id}, tool {scan.ToolId}");

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

    private void Dispose(
        bool disposing)
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