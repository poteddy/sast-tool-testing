using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed partial class ParsingService : IParsingService
{
    private readonly ILogger<ParsingService> _logger;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    private readonly ICweRootCauseResolver
        _rootCauseResolver;

    private bool _disposedValue;

    public ParsingService(
        ILogger<ParsingService> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICweRootCauseResolver rootCauseResolver)
    {
        _logger = logger
            ?? throw new ArgumentNullException(
                nameof(logger));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));

        _rootCauseResolver = rootCauseResolver
            ?? throw new ArgumentNullException(
                nameof(rootCauseResolver));
    }

    public async Task<int> Parse(
        int toolId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            _disposedValue,
            this);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            filePath);

        var normalizedFilePath =
            filePath.Trim('"');

        if (!File.Exists(normalizedFilePath))
        {
            throw new FileNotFoundException(
                "The scanner result file was not found.",
                normalizedFilePath);
        }

        await using var stream =
            File.OpenRead(normalizedFilePath);

        List<CWEs> findings = toolId switch
        {
            1 => await ParseSemgrepAsync(stream),

            2 => await ParseSarifAsync(stream),

            3 => await ParseVeracodeAsync(stream),

            4 => await ParseCppCheckerAsync(stream),

            5 => await ParseCheckmarxAsync(stream),

            6 => await ParseCycodeAsync(stream),
            _ => throw new ArgumentOutOfRangeException(
                nameof(toolId),
                toolId,
                "The specified scanner tool is not supported.")
        };

        return await SaveReportAsync(
            findings,
            toolId,
            GetTruncatedFileName(filePath, 8),
            cancellationToken);
    }

private static string GetTruncatedFileName(string filePath, int maxLength)
{
    var fileName = Path.GetFileNameWithoutExtension(filePath);

    if (fileName.Length <= maxLength)
        return fileName;

    return fileName[..maxLength];
}

private static async Task<List<CWEs>>
        ParseSemgrepAsync(
            Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.SemGrep.Parser();

        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>>
        ParseSarifAsync(
            Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.Sarif.Parser();

        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>>
        ParseVeracodeAsync(
            Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.Veracode.Parser();

        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>>
        ParseCppCheckerAsync(
            Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.CPPChecker.Parser();

        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>>
        ParseCheckmarxAsync(
            Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.Checkmarx.Parser();

        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>>
    ParseCycodeAsync(
        Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.Cycode.Parser();

        return await parser.Get_findings(stream);
    }


    private async Task<int> SaveReportAsync(
        IReadOnlyCollection<CWEs> cwes,
        int toolId,
        string scanName ,
        CancellationToken cancellationToken)
    {
        if (cwes.Count == 0)
        {
            return -1;
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        /*
         * Validate the Tool foreign key before creating the Scan.
         */
        var toolExists = await context.Tools
            .AsNoTracking()
            .AnyAsync(
                tool => tool.Id == toolId,
                cancellationToken);

        if (!toolExists)
        {
            throw new InvalidOperationException(
                $"Tool {toolId} does not exist. " +
                "Ensure the Tool seed completed before parsing.");
        }

        /*
         * Create the parent Scan entity.
         *
         * Do not calculate Scan.Id manually. SQLite generates the
         * key when SaveChangesAsync inserts the Scan.
         */
        var scan = new Scan
        {            
            Name = scanName,
            ToolId = toolId,
            TestResults = []
        };

        foreach (var cweResult in cwes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groundTruthCwe =
                ExtractGroundTruthCwe(
                    cweResult.FilePath);

            if (!groundTruthCwe.HasValue)
            {
                _logger.LogWarning(
                    "No ground-truth CWE was found in " +
                    "file path {FilePath}.",
                    cweResult.FilePath);

                continue;
            }

            

            var scannerCwe =
                cweResult.Cwe;
        
        
            var result = new CWETestResult
            {
                TestPathListedCWE =
                    groundTruthCwe.Value,

                ScannerFoundCWE =
                    scannerCwe,

                Cve =
                    cweResult.Cve ??
                    string.Empty,

                Date =
                    DateTime.UtcNow,

                Description =
                    cweResult.Description ??
                    string.Empty,

                DynamicFinding =
                    cweResult.DynamicFinding,

                FilePath =
                    cweResult.FilePath,

                FoundBy =
                    cweResult.FoundBy ?? [],

                Line =
                    cweResult.Line,

                Mitigation =
                    cweResult.Mitigation ??
                    string.Empty,

                NumericalSeverity =
                    cweResult.NumericalSeverity ??
                    string.Empty,

                References =
                    cweResult.References ??
                    string.Empty,

                Severity =
                    cweResult.Severity ??
                    string.Empty,

                StaticFinding =
                    cweResult.StaticFinding,

                Test =
                    cweResult.Test,

                Title =
                    cweResult.Title ??
                    string.Empty,

                VulnIdFromTool =
                    cweResult.VulnIdFromTool ??
                    string.Empty,

                /*
                 * Establish the relationship using the navigation.
                 *
                 * EF assigns ScanId after the Scan row receives its
                 * generated primary key.
                 */
                Scan =
                    scan
            };

            scan.TestResults.Add(result);

            _logger.LogDebug(
                "Prepared scanner finding for CWE-{ScannerCwe}, " +
                "ground truth CWE-{GroundTruthCwe}, ",
                scannerCwe,
                groundTruthCwe.Value
                );
        }

        if (scan.TestResults.Count == 0)
        {
            _logger.LogWarning(
                "No scanner findings with valid ground-truth " +
                "CWE values were available for tool {ToolId}.",
                toolId);

            return -1;
        }

        /*
         * Add the root of the entity graph.
         *
         * EF discovers and tracks the CWETestResult children through
         * Scan.TestResults. There is no need to call AddRange on the
         * test results separately.
         */
        context.Scans.Add(scan);

        try
        {
            await context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Saved {Count} findings for tool {ToolId} " +
                "as scan {ScanId}.",
                scan.TestResults.Count,
                toolId,
                scan.Id);

            return scan.Id;
        }
        catch (DbUpdateException exception)
        {
            LogFailedEntities(
                context,
                exception);

            _logger.LogError(
                exception,
                "Failed to save CWE results for tool {ToolId}.",
                toolId);

            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to save CWE results for tool {ToolId}.",
                toolId);

            throw;
        }
    }

    private void LogFailedEntities(
        ApplicationDbContext context,
        DbUpdateException exception)
    {
        var entries = exception.Entries.Count > 0
            ? exception.Entries
            : context.ChangeTracker
                .Entries()
                .Where(entry =>
                    entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified)
                .ToList();

        foreach (var entry in entries)
        {
            _logger.LogError(
                "Failed entity {EntityType} in state {State}.",
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
                var foreignKeyValues =
                    foreignKey.Properties
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
                    "{PrincipalEntity}: {ForeignKeyValues}.",
                    entry.Metadata.DisplayName(),
                    foreignKey.PrincipalEntityType.DisplayName(),
                    string.Join(
                        ", ",
                        foreignKeyValues));
            }
        }
    }

    private static int? ExtractGroundTruthCwe(
        string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var match =
            CweFromPathRegex().Match(filePath);

        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(
            match.Groups["number"].Value,
            out var cweId)
                ? cweId
                : null;
    }

    [GeneratedRegex(
        @"CWE[-_ ]?(?<number>\d+)",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant)]
    private static partial Regex CweFromPathRegex();

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