using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class ParsingService : IParsingService
{
    private readonly ILogger<ParsingService> _logger;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    private readonly IJulietPathNormalizer
        _julietPathNormalizer;

    private bool _disposedValue;

    public ParsingService(
        ILogger<ParsingService> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IJulietPathNormalizer julietPathNormalizer)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(nameof(contextFactory));

        _julietPathNormalizer = julietPathNormalizer
            ?? throw new ArgumentNullException(
                nameof(julietPathNormalizer));
    }

    public async Task<int> Parse(
        int toolId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            _disposedValue,
            this);

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var resultFilePath =
            filePath.Trim().Trim('"');

        if (!File.Exists(resultFilePath))
        {
            throw new FileNotFoundException(
                "The scanner result file was not found.",
                resultFilePath);
        }

        await using var stream =
            File.OpenRead(resultFilePath);

        List<CWEs> findings = toolId switch
        {
            1 => await ParseSemgrepAsync(stream),

            2 => await ParseSarifAsync(stream),

            3 => await ParseVeracodeAsync(stream),

            4 => await ParseCppCheckerAsync(stream),

            5 => await ParseCheckmarxAsync(stream),

            6 => await ParseCycodeAsync(stream),

            7 => await ParseOpenVINOAsync(stream),

            8 => await ParseGitlabAsync(stream),
            _ => throw new ArgumentOutOfRangeException(
                nameof(toolId),
                toolId,
                "The specified scanner tool is not supported.")
        };

        return await SaveReportAsync(
            findings,
            toolId,
            GetTruncatedFileName(
                resultFilePath,
                8),
            cancellationToken);
    }

    private static string GetTruncatedFileName(
        string filePath,
        int maxLength)
    {
        var fileName =
            Path.GetFileNameWithoutExtension(filePath);

        if (fileName.Length <= maxLength)
        {
            return fileName;
        }

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
    private static async Task<List<CWEs>>
        ParseOpenVINOAsync(
            Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.OpenVINO.Parser();

        return await parser.Get_findings(stream);
    }
    private static async Task<List<CWEs>>
        ParseGitlabAsync(
            Stream stream)
    {
        using var parser =
            new ToolTester.Parsers.GitLabSast.Parser();

        return await parser.Get_findings(stream);
    }

    private async Task<int> SaveReportAsync(
        IReadOnlyCollection<CWEs> cwes,
        int toolId,
        string scanName,
        CancellationToken cancellationToken)
    {
        if (cwes.Count == 0)
        {
            _logger.LogWarning(
                "The scanner result contained no findings " +
                "for tool {ToolId}.",
                toolId);

            return -1;
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        var toolExists =
            await context.Tools
                .AsNoTracking()
                .AnyAsync(
                    tool => tool.Id == toolId,
                    cancellationToken);

        if (!toolExists)
        {
            throw new InvalidOperationException(
                $"Tool {toolId} does not exist. " +
                "Ensure the tool seed completed before parsing.");
        }

        /*
         * We only need the JulietTest values and IDs.
         *
         * AsNoTracking avoids tracking the entire Juliet catalog.
         * The result relationship is established explicitly with
         * CWETestResult.TestId.
         */
        var julietTests =
            await context.JulietTests
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        if (julietTests.Count == 0)
        {
            throw new InvalidOperationException(
                "No Juliet tests exist in the database. " +
                "Run the Juliet seed before importing scanner results.");
        }

        var testsByNormalizedPath =
            julietTests
                .Where(test =>
                    !string.IsNullOrWhiteSpace(
                        test.NormalizedPath))
                .GroupBy(
                    test =>
                        _julietPathNormalizer.Normalize(
                            test.NormalizedPath),
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.Ordinal);

        var testsByFileName =
            julietTests
                .Where(test =>
                    !string.IsNullOrWhiteSpace(
                        test.FileName))
                .GroupBy(
                    test =>
                        _julietPathNormalizer.GetFileName(
                            test.FileName),
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.Ordinal);

        var scan = new Scan
        {
            Name = scanName,
            ToolId = toolId,
            TestResults = []
        };

        foreach (var cweResult in cwes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resolution =
                ResolveJulietTest(
                    cweResult.FilePath,
                    testsByNormalizedPath,
                    testsByFileName);

            if (resolution.Test is null)
            {
                _logger.LogWarning(
                    "Scanner path {FilePath} normalized to " +
                    "{NormalizedPath}, but could not be resolved " +
                    "to a known Juliet test. Reason: {Reason}.",
                    cweResult.FilePath,
                    resolution.NormalizedPath,
                    resolution.FailureReason);

                continue;
            }

            var julietTest =
                resolution.Test;

            var result = new CWETestResult
            {
                /*
                 * ScannerFoundCWE remains nullable.
                 *
                 * A finding can exist even when the scanner
                 * did not report a CWE.
                 */
                ScannerFoundCWE =
                    cweResult.Cwe,

                Cve =
                    cweResult.Cve
                    ?? string.Empty,

                Date =
                    cweResult.Date
                    ?? DateTime.UtcNow,

                Description =
                    cweResult.Description
                    ?? string.Empty,

                DynamicFinding =
                    cweResult.DynamicFinding,

                /*
                 * Preserve the scanner's original path.
                 *
                 * FilePath is evidence, not the relational
                 * identity of the Juliet test.
                 */
                FilePath =
                    cweResult.FilePath,

                FoundBy =
                    cweResult.FoundBy
                    ?? [],

                Line =
                    cweResult.Line,

                Mitigation =
                    cweResult.Mitigation
                    ?? string.Empty,

                NumericalSeverity =
                    cweResult.NumericalSeverity
                    ?? string.Empty,

                References =
                    cweResult.References
                    ?? string.Empty,

                Severity =
                    cweResult.Severity
                    ?? string.Empty,

                StaticFinding =
                    cweResult.StaticFinding,

                Title =
                    cweResult.Title
                    ?? string.Empty,

                VulnIdFromTool =
                    cweResult.VulnIdFromTool
                    ?? string.Empty,

                /*
                 * This is now the authoritative JulietTest
                 * relationship.
                 */
                TestId =
                    julietTest.Id,

                /*
                 * EF Core assigns ScanId after inserting Scan.
                 */
                Scan =
                    scan
            };

            scan.TestResults.Add(result);

            _logger.LogDebug(
                "Prepared finding for Juliet TestId {TestId}, " +
                "ground-truth CWE-{GroundTruthCwe}, " +
                "scanner CWE-{ScannerCwe}, " +
                "canonical path {CanonicalPath}, " +
                "scanner path {ScannerPath}.",
                julietTest.Id,
                julietTest.CweId,
                cweResult.Cwe,
                julietTest.NormalizedPath,
                cweResult.FilePath);
        }

        if (scan.TestResults.Count == 0)
        {
            _logger.LogWarning(
                "No scanner findings could be resolved to known " +
                "Juliet tests for tool {ToolId}.",
                toolId);

            return -1;
        }

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

    private JulietTestResolution ResolveJulietTest(
        string? scannerFilePath,
        IReadOnlyDictionary<string, List<JulietTest>>
            testsByNormalizedPath,
        IReadOnlyDictionary<string, List<JulietTest>>
            testsByFileName)
    {
        if (string.IsNullOrWhiteSpace(scannerFilePath))
        {
            return new JulietTestResolution(
                Test: null,
                NormalizedPath: string.Empty,
                FailureReason: "The scanner filepath is empty.");
        }

        var normalizedPath =
            _julietPathNormalizer.Normalize(
                scannerFilePath);

        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return new JulietTestResolution(
                Test: null,
                NormalizedPath: normalizedPath,
                FailureReason:
                    "The scanner filepath normalized to an empty string.");
        }

        /*
         * A directory identifies a location, not one Juliet test.
         *
         * Example:
         *
         * testcases/cwe134_uncontrolled_format_string/s03/
         *
         * More than one source file may exist below that directory.
         */
        if (normalizedPath.EndsWith(
                "/",
                StringComparison.Ordinal))
        {
            return new JulietTestResolution(
                Test: null,
                NormalizedPath: normalizedPath,
                FailureReason:
                    "The scanner returned a directory path instead " +
                    "of a source-file path.");
        }

        /*
         * 1. Exact canonical-path match.
         */
        if (testsByNormalizedPath.TryGetValue(
                normalizedPath,
                out var exactMatches))
        {
            if (exactMatches.Count == 1)
            {
                return new JulietTestResolution(
                    Test: exactMatches[0],
                    NormalizedPath: normalizedPath,
                    FailureReason: null);
            }

            return new JulietTestResolution(
                Test: null,
                NormalizedPath: normalizedPath,
                FailureReason:
                    $"The normalized path matched " +
                    $"{exactMatches.Count} Juliet tests.");
        }

        /*
         * 2. Path-suffix match.
         *
         * This handles scanner paths that include or omit
         * different repository-root prefixes.
         */
        var suffixMatches =
            testsByNormalizedPath
                .Where(pair =>
                    PathEndsWithSegment(
                        normalizedPath,
                        pair.Key) ||
                    PathEndsWithSegment(
                        pair.Key,
                        normalizedPath))
                .SelectMany(pair => pair.Value)
                .GroupBy(test => test.Id)
                .Select(group => group.First())
                .ToList();

        if (suffixMatches.Count == 1)
        {
            return new JulietTestResolution(
                Test: suffixMatches[0],
                NormalizedPath: normalizedPath,
                FailureReason: null);
        }

        if (suffixMatches.Count > 1)
        {
            return new JulietTestResolution(
                Test: null,
                NormalizedPath: normalizedPath,
                FailureReason:
                    $"The path matched {suffixMatches.Count} " +
                    "Juliet tests by suffix.");
        }

        /*
         * 3. Unique filename fallback.
         */
        var fileName =
            _julietPathNormalizer.GetFileName(
                scannerFilePath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new JulietTestResolution(
                Test: null,
                NormalizedPath: normalizedPath,
                FailureReason:
                    "No source filename could be extracted.");
        }

        if (!testsByFileName.TryGetValue(
                fileName,
                out var fileNameMatches))
        {
            return new JulietTestResolution(
                Test: null,
                NormalizedPath: normalizedPath,
                FailureReason:
                    $"No Juliet test has filename '{fileName}'.");
        }

        if (fileNameMatches.Count == 1)
        {
            return new JulietTestResolution(
                Test: fileNameMatches[0],
                NormalizedPath: normalizedPath,
                FailureReason: null);
        }

        return new JulietTestResolution(
            Test: null,
            NormalizedPath: normalizedPath,
            FailureReason:
                $"Filename '{fileName}' matched " +
                $"{fileNameMatches.Count} Juliet tests.");
    }

    private static bool PathEndsWithSegment(
        string path,
        string suffix)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            string.IsNullOrWhiteSpace(suffix))
        {
            return false;
        }

        var normalizedPath =
            path.TrimEnd('/');

        var normalizedSuffix =
            suffix.Trim('/');

        if (normalizedPath.Equals(
                normalizedSuffix,
                StringComparison.Ordinal))
        {
            return true;
        }

        return normalizedPath.EndsWith(
            "/" + normalizedSuffix,
            StringComparison.Ordinal);
    }

    private void LogFailedEntities(
        ApplicationDbContext context,
        DbUpdateException exception)
    {
        var entries =
            exception.Entries.Count > 0
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
                            var value =
                                entry
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

    private readonly record struct JulietTestResolution(
        JulietTest? Test,
        string NormalizedPath,
        string? FailureReason);
}