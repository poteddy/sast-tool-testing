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
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICweRootCauseResolver _rootCauseResolver;

    private bool _disposedValue;

    public ParsingService(
        ILogger<ParsingService> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICweRootCauseResolver rootCauseResolver)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(nameof(contextFactory));

        _rootCauseResolver = rootCauseResolver
            ?? throw new ArgumentNullException(nameof(rootCauseResolver));
    }

    public async Task<int> Parse(
        int toolId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var normalizedFilePath = filePath.Trim('"');

        if (!File.Exists(normalizedFilePath))
        {
            throw new FileNotFoundException(
                "The scanner result file was not found.",
                normalizedFilePath);
        }

        await using var stream = File.OpenRead(normalizedFilePath);

        List<CWEs> findings = toolId switch
        {
            1 => await ParseSemgrepAsync(stream),
            2 => await ParseSarifAsync(stream),
            3 => await ParseVeracodeAsync(stream),
            4 => await ParseCppCheckerAsync(stream),
            5 => await ParseCheckmarxAsync(stream),

            _ => throw new ArgumentOutOfRangeException(
                nameof(toolId),
                toolId,
                "The specified scanner tool is not supported.")
        };

        return await SaveReportAsync(
            findings,
            toolId,
            cancellationToken);
    }

    private static async Task<List<CWEs>> ParseSemgrepAsync(Stream stream)
    {
        using var parser = new ToolTester.Parsers.SemGrep.Parser();
        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>> ParseSarifAsync(Stream stream)
    {
        using var parser = new ToolTester.Parsers.Sarif.Parser();
        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>> ParseVeracodeAsync(Stream stream)
    {
        using var parser = new ToolTester.Parsers.Veracode.Parser();
        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>> ParseCppCheckerAsync(Stream stream)
    {
        using var parser = new ToolTester.Parsers.CPPChecker.Parser();
        return await parser.Get_findings(stream);
    }

    private static async Task<List<CWEs>> ParseCheckmarxAsync(Stream stream)
    {
        using var parser = new ToolTester.Parsers.Checkmarx.Parser();
        return await parser.Get_findings(stream);
    }

    private async Task<int> SaveReportAsync(
        IReadOnlyCollection<CWEs> cwes,
        int toolId,
        CancellationToken cancellationToken)
    {
        if (cwes.Count == 0)
        {
            return -1;
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var scanId = await GetNextScanIdAsync(
            context,
            cancellationToken);

        var results = new List<CWETestResult>(cwes.Count);

        foreach (var cweResult in cwes)
        {
            var groundTruthCwe = ExtractGroundTruthCwe(
                cweResult.FilePath);

            if (!groundTruthCwe.HasValue)
            {
                _logger.LogWarning(
                    "No ground-truth CWE was found in file path {FilePath}.",
                    cweResult.FilePath);

                continue;
            }

            var scannerCwe = cweResult.Cwe;

            int? rootCauseCwe = null;

            if (scannerCwe > 0)
            {
                rootCauseCwe =
                    await _rootCauseResolver.ResolveRootCauseAsync(
                        scannerCwe,
                        groundTruthCwe.Value,
                        cancellationToken);
            }

            var result = new CWETestResult
            {
                TestPathListedCWE = groundTruthCwe.Value,
                ScannerFoundCWE = scannerCwe,
                RootCauseCWE = rootCauseCwe,

                Cve = cweResult.Cve ?? string.Empty,
                Date = DateTime.UtcNow,
                Description = cweResult.Description ?? string.Empty,
                DynamicFinding = cweResult.DynamicFinding,
                FilePath = cweResult.FilePath ?? string.Empty,
                FoundBy = cweResult.FoundBy,
                Line = cweResult.Line,
                Mitigation = cweResult.Mitigation ?? string.Empty,
                NumericalSeverity = cweResult.NumericalSeverity,
                References = cweResult.References ?? string.Empty,
                Severity = cweResult.Severity ?? string.Empty,
                StaticFinding = cweResult.StaticFinding,
                Test = cweResult.Test,
                Title = cweResult.Title ?? string.Empty,
                VulnIdFromTool =
                    cweResult.VulnIdFromTool ?? string.Empty,

                ScanId = scanId
            };

            results.Add(result);

            //_logger.LogDebug(
            //    "CWE result: scanner CWE-{ScannerCwe}, " +
            //    "ground truth CWE-{GroundTruthCwe}, " +
            //    "root cause CWE-{RootCauseCwe}.",
            //    scannerCwe,
            //    groundTruthCwe,
            //    rootCauseCwe);
        }

        if (results.Count == 0)
        {
            return -1;
        }

        try
        {
            await context.CWETestResults.AddRangeAsync(
                results,
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved {Count} findings for tool {ToolId} as scan {ScanId}.",
                results.Count,
                toolId,
                scanId);

            return scanId;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to save CWE results for tool {ToolId} and scan {ScanId}.",
                toolId,
                scanId);

            throw;
        }
    }

    private static async Task<int> GetNextScanIdAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var maximumScanId =
            await context.CWETestResults
                .Select(result => (int?)result.ScanId)
                .MaxAsync(cancellationToken);

        return maximumScanId.GetValueOrDefault() + 1;
    }

    private static int? ExtractGroundTruthCwe(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var match = CweFromPathRegex().Match(filePath);

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